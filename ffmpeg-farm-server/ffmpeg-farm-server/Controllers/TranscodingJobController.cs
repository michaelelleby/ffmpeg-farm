using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Http;
using Contract;
using Dapper;

namespace ffmpeg_farm_server.Controllers
{
    public class TranscodingJobController : ApiController
    {
        [HttpGet]
        public TranscodingJob GetNextJob()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var transaction = connection.BeginTransaction();
                try
                {
                    var data = connection.Query("SELECT Id, Arguments, JobCorrelationId FROM FfmpegJobs WHERE Taken = 0 ORDER BY Needed ASC LIMIT 1;")
                            .FirstOrDefault();
                    if (data != null)
                    {
                        var rowsUpdated = connection.Execute("UPDATE FfmpegJobs SET Taken = 1 WHERE Id = @Id;", new {data.Id});
                        if (rowsUpdated == 0)
                            throw new Exception("Failed to mark row as taken");

                        transaction.Commit();

                        return new TranscodingJob
                        {
                            Id = Convert.ToInt32(data.Id),
                            Arguments = data.Arguments,
                            JobCorrelationId = data.JobCorrelationId
                        };

                    }
                }
                catch
                {
                    transaction.Rollback();

                    throw;
                }

                return null;
            }
        }

        [HttpPost]
        public void QueueNew(JobRequest job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.SourceFilename))
                throw new ArgumentException("SourceFilename is a required parameter.");
            if (!File.Exists(job.SourceFilename))
                throw new FileNotFoundException("SourceFilename does not exist", job.SourceFilename);

            int duration = GetDuration(job);

            string destinationFormat = Path.GetExtension(job.DestinationFilename);
            string destinationFolder = Path.GetDirectoryName(job.DestinationFilename);
            string destinationFilenamePrefix = Path.GetFileNameWithoutExtension(job.DestinationFilename);

            if (!Directory.Exists(destinationFolder))
                throw new ArgumentException($@"Destination folder {destinationFolder} does not exist.");

            using (var connection = GetConnection())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                try
                {
                    const int chunkDuration = 60;

                    var jobCorrelationId = Guid.NewGuid();

                    connection.Execute(
                        "INSERT INTO FfmpegRequest (JobCorrelationId, SourceFilename, DestinationFilename, Needed) VALUES(?, ?, ?, ?);",
                        new {jobCorrelationId, job.SourceFilename, job.DestinationFilename, job.Needed});
                    
                    for (int i = 0; duration - i*chunkDuration > 0; i++)
                    {
                        int value = i*chunkDuration;
                        if (value > duration)
                        {
                            value = duration;
                        }

                        string arguments = $@"-y -ss {TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.SourceFilename}""";

                        for (int j = 0; j < job.Targets.Length; j++)
                        {
                            DestinationFormat target = job.Targets[j];
                            
                            string chunkFilename = $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{j}_{value}.{destinationFormat}";
                            arguments +=
                                $@" -s {target.Width}x{target.Height} -profile:v high -b:v {target.VideoBitrate}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";

                            connection.Execute(
                                "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(?, ?, ?, ?);",
                                new {jobCorrelationId, j, chunkFilename, i});
                        }

                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename) VALUES(?, ?, ?, ?);",
                            new
                            {jobCorrelationId, arguments, job.Needed, job.SourceFilename});
                    }

                    for (int i = 0; i < job.Targets.Length; i++)
                    {
                        string targetFilename = $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{i}.aac";
                        string arguments = $@"-y -t {duration} -i ""{job.SourceFilename}"" -vn ""{targetFilename}""";
                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename) VALUES(?, ?, ?, ?);",
                            new {jobCorrelationId, arguments, job.Needed, job.SourceFilename});
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();

                    throw;
                }
            }
        }

        [HttpPut]
        public void ProgressUpdate(TranscodingJob transcodingJob)
        {
            if (transcodingJob == null) throw new ArgumentNullException(nameof(transcodingJob));

            using (var connection = GetConnection())
            {
                connection.Open();

                var transaction = connection.BeginTransaction();
                try
                {
                    var jobRequest = connection.Query<dynamic>(
                        "SELECT JobCorrelationId, SourceFilename, DestinationFilename, Needed FROM FfmpegRequest WHERE JobCorrelationId = ?",
                        new {transcodingJob.JobCorrelationId})
                        .SingleOrDefault();
                    if (jobRequest == null)
                        throw new ArgumentException($@"Job with correlation id {transcodingJob.JobCorrelationId} not found");

                    int updatedRows = connection.Execute(
                            "UPDATE FfmpegJobs SET Progress = @Progress, Heartbeat = @Heartbeat, Done = @Done WHERE Id = @Id;", new
                            {
                                Id = transcodingJob.Id,
                                Progress = transcodingJob.Progress.TotalSeconds,
                                Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                                Done = transcodingJob.Done
                            });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {transcodingJob.Id}");

                    if (connection.Query<int>(
                        "SELECT COUNT(*) FROM FfmpegJobs WHERE JobCorrelationId = ? AND Done = 0;",
                        new {jobRequest.JobCorrelationId})
                        .Single() == 0)
                    {
                        var chunks = connection.Query<FfmpegPart>(
                            "SELECT Filename, Number, Target, (SELECT SourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS SourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                            new {Id = transcodingJob.JobCorrelationId});

                        string path = string.Format("{0}{1}{2}.list", Path.GetDirectoryName(jobRequest.DestinationFilename), Path.DirectorySeparatorChar,
                            Path.GetFileNameWithoutExtension(jobRequest.DestinationFilename));
                        foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
                        {
                            using (TextWriter tw = new StreamWriter(path))
                            {
                                foreach (FfmpegPart part in chunk)
                                {
                                    tw.WriteLine($"file '{part.Filename}'");
                                }
                            }
                            string arguments = $@"-y -f concat -safe 0 -i ""{path}"" -i ""{jobRequest.SourceFilename}"" -c:v copy {jobRequest.DestinationFilename}";

                            connection.Execute(
                                "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename) VALUES(?, ?, ?, ?);",
                                new
                                {
                                    transcodingJob.JobCorrelationId,
                                    arguments,
                                    DateTimeOffset.MaxValue,
                                    jobRequest.SourceFilename
                                });
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();

                    throw;
                }
            }
        }

        private static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString);
        }

        private static int GetDuration(JobRequest job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (!File.Exists(job.SourceFilename))
                throw new FileNotFoundException("Media not found when trying to get file duration", job.SourceFilename);

            string mediaInfoPath = ConfigurationManager.AppSettings["MediaInfoPath"];
            if (!File.Exists(mediaInfoPath))
                throw new FileNotFoundException("MediaInfo.exe was not found", mediaInfoPath);

            var mediaInfoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    Arguments = $"--inform=\"General;%Duration%\" \"{job.SourceFilename}\"",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            return Convert.ToInt32(mediaInfoProcess.StandardOutput.ReadToEnd())/1000;
        }
    }
}
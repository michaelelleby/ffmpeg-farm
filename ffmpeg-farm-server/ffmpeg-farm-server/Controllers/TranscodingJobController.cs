using System;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Dapper;

namespace ffmpeg_farm_server.Controllers
{
    public class TranscodingJobController : ApiController
    {
        [HttpGet]
        public TranscodingJob GetNextJob(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                InsertClientHeartbeat(machineName, connection);

                var transaction = connection.BeginTransaction();
                try
                {
                    int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
                    DateTime timeout = DateTimeOffset.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

                    var data = connection.Query("SELECT Id, Arguments, JobCorrelationId FROM FfmpegJobs WHERE Done = 0 AND (Taken = 0 OR HeartBeat < ?) ORDER BY Needed ASC LIMIT 1;",
                        new {timeout})
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

        private static void InsertClientHeartbeat(string machineName, IDbConnection connection)
        {
            connection.Execute("INSERT OR REPLACE INTO Clients (MachineName, LastHeartbeat) VALUES(?, ?);",
                new {machineName, DateTime.UtcNow});
        }

        [HttpPost]
        public void QueueNew(JobRequest job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.SourceFilename))
                throw new ArgumentException("SourceFilename is a required parameter.");
            if (!File.Exists(job.SourceFilename))
                throw new FileNotFoundException("SourceFilename does not exist", job.SourceFilename);

            int duration = GetDuration(job.SourceFilename);

            string destinationFormat = Path.GetExtension(job.DestinationFilename);
            string destinationFolder = Path.GetDirectoryName(job.DestinationFilename);
            string destinationFilenamePrefix = Path.GetFileNameWithoutExtension(job.DestinationFilename);

            if (!Directory.Exists(destinationFolder))
                throw new ArgumentException($@"Destination folder {destinationFolder} does not exist.");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                var transaction = connection.BeginTransaction();
                try
                {
                    const int chunkDuration = 60;

                    var jobCorrelationId = Guid.NewGuid();

                    connection.Execute(
                        "INSERT INTO FfmpegRequest (JobCorrelationId, SourceFilename, DestinationFilename, Needed, Created) VALUES(?, ?, ?, ?, ?);",
                        new {jobCorrelationId, job.SourceFilename, job.DestinationFilename, job.Needed, DateTime.Now});

                    // Queue audio first because it cannot be chunked and thus will take longer to transcode
                    // and if we do it first chances are it will be ready when all the video parts are ready
                    for (int i = 0; i < job.Targets.Length; i++)
                    {
                        DestinationFormat format = job.Targets[i];

                        string chunkFilename = $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{i}_audio.mp4";
                        string arguments = $@"-y -i ""{job.SourceFilename}"" -c:a aac -b:a {format.AudioBitrate}k -vn ""{chunkFilename}""";

                        const int number = 0;
                        connection.Execute(
                            "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(?, ?, ?, ?);",
                            new { jobCorrelationId, i, chunkFilename, number });

                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                            new
                            { jobCorrelationId, arguments, job.Needed, job.SourceFilename, duration});
                    }

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

                            string chunkFilename = $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{j}_{value}{destinationFormat}";

                            if (Convert.ToBoolean(ConfigurationManager.AppSettings["EnableCrf"]))
                            {
                                int bufSize = target.VideoBitrate / 8 * chunkDuration;
                                arguments += $@" -s {target.Width}x{target.Height} -c:v libx264 -profile:v high -crf 18 -preset medium -maxrate {target.VideoBitrate}k -bufsize {bufSize}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                            }
                            else
                            {
                                arguments += $@" -s {target.Width}x{target.Height} -c:v libx264 -profile:v high -b:v {target.VideoBitrate}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                            }

                            connection.Execute(
                                "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(?, ?, ?, ?);",
                                new {jobCorrelationId, j, chunkFilename, i});
                        }

                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                            new
                            {jobCorrelationId, arguments, job.Needed, job.SourceFilename, chunkDuration});
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
            if (string.IsNullOrWhiteSpace(transcodingJob.MachineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                InsertClientHeartbeat(transcodingJob.MachineName, connection);

                var transaction = connection.BeginTransaction();
                try
                {
                    connection.Execute("INSERT OR REPLACE INTO Clients (MachineName, LastHeartbeat) VALUES(?, ?);",
                        new {transcodingJob.MachineName, DateTimeOffset.UtcNow});

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

                    FileInfo fileInfo = new FileInfo(jobRequest.DestinationFilename);
                    if (fileInfo.Exists == false &&
                        connection.Query<int>(
                            "SELECT COUNT(*) FROM FfmpegJobs WHERE JobCorrelationId = ? AND Done = 0;",
                            new {jobRequest.JobCorrelationId})
                            .Single() == 0)
                    {
                        var chunks = connection.Query<FfmpegPart>(
                            "SELECT Filename, Number, Target, (SELECT SourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS SourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                            new {Id = transcodingJob.JobCorrelationId});

                        foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
                        {
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jobRequest.DestinationFilename);
                            string fileExtension = Path.GetExtension(jobRequest.DestinationFilename);
                            string outputFolder = Path.GetDirectoryName(jobRequest.DestinationFilename);
                            int targetNumber = chunk.First().Target;

                            string targetFilename =
                                $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{targetNumber}{fileExtension}";

                            // TODO Implement proper detection if files are already merged
                            if (File.Exists(targetFilename))
                                continue;

                            string path = string.Format("{0}{1}{2}_{3}.list",
                                outputFolder,
                                Path.DirectorySeparatorChar,
                                fileNameWithoutExtension,
                                targetNumber);

                            using (TextWriter tw = new StreamWriter(path))
                            {
                                foreach (FfmpegPart part in chunk.Where(x => x.IsAudio == false))
                                {
                                    tw.WriteLine($"file '{part.Filename}'");
                                }
                            }
                            string audioSource = chunk.Single(x => x.IsAudio).Filename;

                            string arguments =
                                $@"-y -f concat -safe 0 -i ""{path}"" -i ""{audioSource}"" -c copy {targetFilename}";

                            int duration = GetDuration(jobRequest.SourceFilename);
                            connection.Execute(
                                "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, SourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                                new
                                {
                                    transcodingJob.JobCorrelationId,
                                    arguments,
                                    jobRequest.Needed,
                                    jobRequest.SourceFilename,
                                    duration
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

        private static int GetDuration(string sourceFilename)
        {
            if (string.IsNullOrWhiteSpace(sourceFilename)) throw new ArgumentNullException("sourceFilename");
            if (!File.Exists(sourceFilename))
                throw new FileNotFoundException("Media not found when trying to get file duration", sourceFilename);

            string mediaInfoPath = ConfigurationManager.AppSettings["MediaInfoPath"];
            if (!File.Exists(mediaInfoPath))
                throw new FileNotFoundException("MediaInfo.exe was not found", mediaInfoPath);

            var mediaInfoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    Arguments = $"--inform=\"General;%Duration%\" \"{sourceFilename}\"",
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
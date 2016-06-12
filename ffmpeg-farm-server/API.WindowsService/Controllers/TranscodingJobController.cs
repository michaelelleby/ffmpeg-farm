using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class TranscodingJobController : ApiController
    {
        /// <summary>
        /// Get next transcoding job
        /// </summary>
        /// <param name="machineName">Client's machine name used to stamp who took the job</param>
        /// <returns><see cref="TranscodingJob"/></returns>
        public BaseJob GetNextJob(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            Helper.InsertClientHeartbeat(machineName);

            Mp4boxJob dashJob = GetNextDashJob();
            if (dashJob != null)
                return dashJob;

            return GetNextMergeJob() ?? GetTranscodingJob();
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId">Job id returned when creating new job</param>
        [HttpDelete]
        public void Delete(Guid jobId)
        {
            if (jobId == Guid.Empty)
                throw new ArgumentException("Job id must be a valid GUID.");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    int rowsDeleted = connection.Execute("DELETE FROM FfmpegRequest WHERE JobCorrelationId = ?;",
                        new { jobId });
                    if (rowsDeleted != 1)
                        throw new ArgumentException($@"No job with id {jobId} found.");

                    connection.Execute("DELETE FROM FfmpegJobs WHERE JobCorrelationId = ?;",
                        new { jobId });

                    connection.Execute("DELETE FROM FfmpegParts WHERE JobCorrelationId = ?;",
                        new { jobId });

                    connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = ?;",
                                            new { jobId });

                    connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = ?;",
                                                                new { jobId });

                    transaction.Commit();
                }
            }
        }

        private TranscodingJob GetNextMergeJob()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
                    DateTime timeout = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

                    var data = connection.Query(
                        "SELECT Id, Arguments, JobCorrelationId FROM FfmpegMergeJobs WHERE Active = 1 AND Done = 0 AND (Taken = 0 OR HeartBeat < ?) ORDER BY Needed ASC LIMIT 1;",
                        new {timeout})
                        .FirstOrDefault();
                    if (data == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    var rowsUpdated =
                        connection.Execute(
                            "UPDATE FfmpegMergeJobs SET Taken = 1, HeartBeat = ? WHERE JobCorrelationId = ?;",
                            new {DateTime.UtcNow, data.JobCorrelationId});
                    if (rowsUpdated == 0)
                    {
                        transaction.Rollback();
                        throw new Exception("Failed to mark row as taken");
                    }

                    transaction.Commit();

                    return new MergeJob
                    {
                        Id = Convert.ToInt32(data.Id),
                        Arguments = data.Arguments,
                        JobCorrelationId = data.JobCorrelationId
                    };
                }
            }
        }

        private static TranscodingJob GetTranscodingJob()
        {
            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            DateTime timeout =
                DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var data = connection.Query(
                        "SELECT Id, Arguments, JobCorrelationId FROM FfmpegJobs WHERE Active = 1 AND Done = 0 AND (Taken = 0 OR HeartBeat < ?) ORDER BY Needed ASC LIMIT 1;",
                        new {timeout})
                        .FirstOrDefault();
                    if (data == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    var rowsUpdated = connection.Execute("UPDATE FfmpegJobs SET Taken = 1, HeartBeat = ? WHERE Id = ?;",
                        new {DateTime.UtcNow, data.Id});
                    if (rowsUpdated == 0)
                    {
                        transaction.Rollback();
                        throw new Exception("Failed to mark row as taken");
                    }

                    transaction.Commit();

                    return new TranscodingJob
                    {
                        Id = Convert.ToInt32(data.Id),
                        Arguments = data.Arguments,
                        JobCorrelationId = data.JobCorrelationId
                    };
                }
            }
        }

        private static Mp4boxJob GetNextDashJob()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var data = connection.Query(
                        "SELECT JobCorrelationId, Arguments FROM Mp4boxJobs WHERE Done = 0 ORDER BY Needed ASC LIMIT 1;")
                        .SingleOrDefault();
                    if (data == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    var rowsUpdated = connection.Execute("UPDATE Mp4boxJobs SET Taken = 1 WHERE JobCorrelationId = ?;",
                        new {data.JobCorrelationId});
                    if (rowsUpdated != 1)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    transaction.Commit();

                    return new Mp4boxJob
                    {
                        JobCorrelationId = data.JobCorrelationId,
                        Arguments = data.Arguments
                    };
                }
            }
        }

        /// <summary>
        /// Queue new transcoding job
        /// </summary>
        /// <param name="job"></param>
        public Guid PostQueueNew(JobRequest job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.VideoSourceFilename) && string.IsNullOrWhiteSpace(job.AudioSourceFilename))
                throw new ArgumentException("Either VideoSourceFilename or AudioSourceFilename is a required parameter.");
            if (!string.IsNullOrWhiteSpace(job.VideoSourceFilename) && !File.Exists(job.VideoSourceFilename))
                throw new FileNotFoundException("VideoSourceFilename does not exist", job.VideoSourceFilename);
            if (!string.IsNullOrWhiteSpace(job.AudioSourceFilename) && !File.Exists(job.AudioSourceFilename))
                throw new FileNotFoundException("AudioSourceFilename does not exist", job.AudioSourceFilename);

            int duration = Helper.GetDuration(job.VideoSourceFilename);
            double framerate = Helper.GetFramerate(job.VideoSourceFilename);

            string destinationFormat = Path.GetExtension(job.DestinationFilename);
            string destinationFolder = Path.GetDirectoryName(job.DestinationFilename);
            string destinationFilenamePrefix = Path.GetFileNameWithoutExtension(job.DestinationFilename);

            if (!Directory.Exists(destinationFolder))
                throw new ArgumentException($@"Destination folder {destinationFolder} does not exist.");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    const int chunkDuration = 60;

                    var jobCorrelationId = Guid.NewGuid();

                    connection.Execute(
                        "INSERT INTO FfmpegRequest (JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, Created, EnableDash) VALUES(?, ?, ?, ?, ?, ?, ?);",
                        new
                        {
                            jobCorrelationId,
                            job.VideoSourceFilename,
                            job.AudioSourceFilename,
                            job.DestinationFilename,
                            job.Needed,
                            DateTime.Now,
                            job.EnableDash
                        });

                    // Queue audio first because it cannot be chunked and thus will take longer to transcode
                    // and if we do it first chances are it will be ready when all the video parts are ready
                    for (int i = 0; i < job.Targets.Length; i++)
                    {
                        DestinationFormat format = job.Targets[i];

                        string chunkFilename =
                            $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{i}_audio.mp4";
                        string source = job.HasAlternateAudio
                            ? job.AudioSourceFilename
                            : job.VideoSourceFilename;

                        string arguments =
                            $@"-y -i ""{source}"" -c:a aac -b:a {format.AudioBitrate}k -vn ""{chunkFilename}""";

                        const int number = 0;
                        connection.Execute(
                            "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(?, ?, ?, ?);",
                            new {jobCorrelationId, i, chunkFilename, number});

                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, AudioSourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                            new
                            {jobCorrelationId, arguments, job.Needed, source, duration});
                    }

                    for (int i = 0; duration - i*chunkDuration > 0; i++)
                    {
                        int value = i*chunkDuration;
                        if (value > duration)
                        {
                            value = duration;
                        }

                        string arguments =
                            $@"-y -ss {TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.VideoSourceFilename}""";

                        for (int j = 0; j < job.Targets.Length; j++)
                        {
                            DestinationFormat target = job.Targets[j];

                            string chunkFilename =
                                $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{j}_{value}{destinationFormat}";

                            if (job.EnableDash)
                            {
                                arguments += $@" -s {target.Width}x{target.Height} -c:v libx264 -g {framerate*4}";
                                arguments += $@" -keyint_min {framerate*4} -profile:v high -b:v {target.VideoBitrate}k";
                                arguments += $@" -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                            }
                            else
                            {
                                if (Convert.ToBoolean(ConfigurationManager.AppSettings["EnableCrf"]))
                                {
                                    int bufSize = target.VideoBitrate/8*chunkDuration;
                                    arguments += 
                                        $@" -s {target.Width}x{target.Height} -c:v libx264 -profile:v high -crf 18 -preset medium -maxrate {target
                                            .VideoBitrate}k -bufsize {bufSize}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                                }
                                else
                                {
                                    arguments +=
                                        $@" -s {target.Width}x{target.Height} -c:v libx264 -profile:v high -b:v {target
                                            .VideoBitrate}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                                }
                            }

                            connection.Execute(
                                "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(?, ?, ?, ?);",
                                new {jobCorrelationId, j, chunkFilename, i});
                        }

                        connection.Execute(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                            new
                            {jobCorrelationId, arguments, job.Needed, job.VideoSourceFilename, chunkDuration});
                    }

                    transaction.Commit();

                    return jobCorrelationId;
                }
            }
        }
    }
}
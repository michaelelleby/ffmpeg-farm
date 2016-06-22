using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
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

                using (var scope = new TransactionScope())
                {
                    int rowsDeleted = connection.Execute("DELETE FROM FfmpegRequest WHERE JobCorrelationId = @Id;",
                        new { Id = jobId });
                    if (rowsDeleted != 1)
                        throw new ArgumentException($@"No job with id {jobId} found.");

                    connection.Execute("DELETE FROM FfmpegJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegParts WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    scope.Complete();
                }
            }
        }

        private static MergeJob GetNextMergeJob()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
                    DateTime timeout = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

                    var data = connection.Query<MergeJob>(
                        "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegMergeJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                        new {QueuedState = TranscodingJobState.Queued, InProgressState = TranscodingJobState.InProgress, Heartbeat = timeout})
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegMergeJobs SET State = @State, HeartBeat = @Heartbeat WHERE Id = @Id;",
                        new {State = TranscodingJobState.InProgress, Heartbeat = DateTime.UtcNow, Id = data.Id});
                    if (rowsUpdated == 0)
                    {
                        throw new Exception("Failed to mark row as taken");
                    }

                    scope.Complete();

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

                using (var scope = new TransactionScope())
                {
                    var data = connection.Query<TranscodingJob>(
                        "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                        new { QueuedState = TranscodingJobState.Queued, InProgressState = TranscodingJobState.InProgress, Heartbeat = timeout })
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated = connection.Execute("UPDATE FfmpegJobs SET State = @State, HeartBeat = @Heartbeat, Started = @Heartbeat WHERE Id = @Id;",
                        new {State = TranscodingJobState.InProgress, Heartbeat = DateTime.UtcNow, Id = data.Id});
                    if (rowsUpdated == 0)
                    {
                        throw new Exception("Failed to mark row as taken");
                    }

                    scope.Complete();

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

                using (var scope = new TransactionScope())
                {
                    var data = connection.Query<Mp4boxJob>(
                        "SELECT TOP 1 JobCorrelationId, Arguments FROM Mp4boxJobs WHERE State = @State ORDER BY Needed ASC, Id ASC;",
                        new {State = TranscodingJobState.Queued})
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated = connection.Execute("UPDATE Mp4boxJobs SET State = @State WHERE JobCorrelationId = @Id;",
                        new {State = TranscodingJobState.InProgress, Id = data.JobCorrelationId});
                    if (rowsUpdated != 1)
                    {
                        return null;
                    }

                    scope.Complete();

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

            if (string.IsNullOrWhiteSpace(destinationFormat))
                throw new ArgumentException("DestinationFilename must have an extension to determine the output format.");

            if (!Directory.Exists(destinationFolder))
                throw new ArgumentException($@"Destination folder {destinationFolder} does not exist.");

            ICollection<TranscodingJob> transcodingJobs = new List<TranscodingJob>();
            const int chunkDuration = 60;
            Guid jobCorrelationId = Guid.NewGuid();

            // Queue audio first because it cannot be chunked and thus will take longer to transcode
            // and if we do it first chances are it will be ready when all the video parts are ready
            string source = job.HasAlternateAudio
                ? job.AudioSourceFilename
                : job.VideoSourceFilename;

            TranscodingJob audioJob = new TranscodingJob
            {
                JobCorrelationId = jobCorrelationId,
                SourceFilename = source,
                Needed = job.Needed,
                State = TranscodingJobState.Queued
            };
            string arguments = $@"-y -i ""{source}""";
            for (int i = 0; i < job.Targets.Length; i++)
            {
                DestinationFormat format = job.Targets[i];

                string chunkFilename =
                    $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{i}_audio.mp4";
                arguments += $@" -c:a aac -b:a {format.AudioBitrate}k -vn ""{chunkFilename}""";

                audioJob.Chunks.Add(
                    new FfmpegPart
                    {
                        SourceFilename = source,
                        JobCorrelationId = jobCorrelationId,
                        Filename = chunkFilename,
                        Target = i,
                        Number = 0
                    });
            }
            audioJob.Arguments = arguments;

            transcodingJobs.Add(audioJob);

            for (int i = 0; duration - i*chunkDuration > 0; i++)
            {
                int value = i*chunkDuration;
                if (value > duration)
                {
                    value = duration;
                }

                arguments =
                    $@"-y -ss {TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.VideoSourceFilename}""";

                var transcodingJob = new TranscodingJob
                {
                    JobCorrelationId = jobCorrelationId,
                    SourceFilename = job.VideoSourceFilename,
                    Needed = job.Needed,
                    State = TranscodingJobState.Queued
                };

                for (int j = 0; j < job.Targets.Length; j++)
                {
                    DestinationFormat target = job.Targets[j];

                    string chunkFilename =
                        $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{target.Width}x{target
                            .Height}_{target.VideoBitrate}_{target.AudioBitrate}_{value}{destinationFormat}";

                    if (job.EnableDash)
                    {
                        arguments +=
                            $@" -vf scale={target.Width}x{target.Height} -sws_flags spline -c:v libx264 -g {framerate*4}";
                        arguments += $@" -keyint_min {framerate*4} -profile:v high -b:v {target.VideoBitrate}k";
                        arguments += $@" -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                    }
                    else
                    {
                        arguments +=
                            $@" -vf scale={target.Width}x{target.Height} -sws_flags spline -c:v libx264 -profile:v high -b:v {target
                                .VideoBitrate}k -level 4.1 -pix_fmt yuv420p -an ""{chunkFilename}""";
                    }

                    transcodingJob.Chunks.Add(new FfmpegPart
                    {
                        JobCorrelationId = jobCorrelationId,
                        SourceFilename = job.VideoSourceFilename,
                        Filename = chunkFilename,
                        Target = j,
                        Number = i
                    });
                }

                transcodingJob.Arguments = arguments;

                transcodingJobs.Add(transcodingJob);
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    SaveJobs(job, transcodingJobs, connection, jobCorrelationId, chunkDuration);

                    scope.Complete();

                    return jobCorrelationId;
                }
            }
        }

        private static void SaveJobs(JobRequest job, IEnumerable<TranscodingJob> jobs, IDbConnection connection, Guid jobCorrelationId, int chunkDuration)
        {
            if (jobs.Any(x => x.State == TranscodingJobState.Unknown))
                throw new ArgumentException("One or more jobs have state TranscodingJobState.Unknown. A valid state must be set before saving to database");

            connection.Execute(
                        "INSERT INTO FfmpegRequest (JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, Created, EnableDash) VALUES(@JobCorrelationId, @VideoSourceFilename, @AudioSourceFilename, @DestinationFilename, @Needed, @Created, @EnableDash);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            VideoSourceFilename = job.VideoSourceFilename,
                            AudioSourceFilename = job.AudioSourceFilename,
                            DestinationFilename = job.DestinationFilename,
                            Needed = job.Needed,
                            Created = DateTime.UtcNow,
                            EnableDash = job.EnableDash
                        });

            foreach (DestinationFormat target in job.Targets)
            {
                connection.Execute(
                    "INSERT INTO FfmpegRequestTargets (JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate) VALUES(@JobCorrelationId, @Width, @Height, @VideoBitrate, @AudioBitrate);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Width = target.Width,
                        Height = target.Height,
                        VideoBitrate = target.VideoBitrate,
                        AudioBitrate = target.AudioBitrate
                    });
            }

            foreach (TranscodingJob transcodingJob in jobs)
            {
                connection.Execute(
                    "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @VideoSourceFilename, @ChunkDuration, @State);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Arguments = transcodingJob.Arguments,
                        Needed = transcodingJob.Needed,
                        VideoSourceFilename = transcodingJob.SourceFilename,
                        ChunkDuration = chunkDuration,
                        State = transcodingJob.State
                    });

                foreach (FfmpegPart part in transcodingJob.Chunks)
                {
                    connection.Execute(
                        "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number) VALUES(@JobCorrelationId, @Target, @Filename, @Number);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Target = part.Target,
                            Filename = part.Filename,
                            Number = part.Number
                        });
                }
            }
        }
    }
}
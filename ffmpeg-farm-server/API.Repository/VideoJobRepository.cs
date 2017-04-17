using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Contract;
using Dapper;

namespace API.Repository
{
    public class VideoJobRepository : JobRepository, IVideoJobRepository
    {
        public VideoJobRepository(IHelper helper) : base(helper)
        {
            
        }

        public VideoTranscodingJob GetNextTranscodingJob()
        {
            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            DateTime timeout =
                DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    var data = connection.Query<dynamic>(
                            "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegVideoJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                            new
                            {
                                QueuedState = TranscodingJobState.Queued,
                                InProgressState = TranscodingJobState.InProgress,
                                Heartbeat = timeout
                            })
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var parts =
                        connection.Query<dynamic>(
                            "SELECT Id, JobCorrelationId, Filename, Number, Target, PSNR FROM FfmpegVideoParts WHERE FfmpegVideoJobs_Id = @JobId;",
                            new {JobId = data.Id});

                    var rowsUpdated =
                        connection.Execute(
                            "UPDATE FfmpegVideoJobs SET State = @State, HeartBeat = @Heartbeat, Started = @Heartbeat WHERE Id = @Id;",
                            new {State = TranscodingJobState.InProgress, Heartbeat = DateTime.UtcNow, Id = data.Id});
                    if (rowsUpdated == 0)
                    {
                        throw new Exception("Failed to mark row as taken");
                    }

                    scope.Complete();

                    var job = new VideoTranscodingJob
                    {
                        Id = Convert.ToInt32(data.Id),
                        Arguments = data.Arguments.Split('|'),
                        JobCorrelationId = data.JobCorrelationId,
                        Chunks = parts.Select(x => new FfmpegPart
                        {
                            Id = x.Id,
                            JobCorrelationId = x.JobCorrelationId,
                            Psnr = x.PSNR,
                            Target = x.Target,
                            Number = x.Number,
                            SourceFilename = x.SourceFilename,
                            Filename = x.Filename,
                        }).ToList()
                    };
                    return job;
                }
            }
        }

        public MergeJob GetNextMergeJob()
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
                    DateTime timeout = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

                    var data = connection.Query<dynamic>(
                            "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegMergeJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                            new
                            {
                                QueuedState = TranscodingJobState.Queued,
                                InProgressState = TranscodingJobState.InProgress,
                                Heartbeat = timeout
                            })
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

        public Mp4boxJob GetNextDashJob()
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    var data = connection.Query<Mp4boxJob>(
                            "SELECT TOP 1 JobCorrelationId, Arguments FROM Mp4boxJobs WHERE State = @State ORDER BY Needed ASC, Id ASC;",
                            new {State = TranscodingJobState.Queued})
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated =
                        connection.Execute("UPDATE Mp4boxJobs SET State = @State WHERE JobCorrelationId = @Id;",
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

        public void SaveJobs(JobRequest request, ICollection<VideoTranscodingJob> jobs, IDbConnection connection,
            Guid jobCorrelationId, int chunkDuration)
        {
            if (jobs.Any(x => x.State == TranscodingJobState.Unknown))
                throw new ArgumentException(
                    "One or more jobs have state TranscodingJobState.Unknown. A valid state must be set before saving to database");

            connection.Execute(
                "INSERT INTO FfmpegVideoRequest (JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, Created, EnableDash, EnableTwoPass, EnablePsnr) VALUES(@JobCorrelationId, @VideoSourceFilename, @AudioSourceFilename, @DestinationFilename, @Needed, @Created, @EnableDash, @EnableTwoPass, @EnablePsnr);",
                new
                {
                    JobCorrelationId = jobCorrelationId,
                    request.VideoSourceFilename,
                    request.AudioSourceFilename,
                    request.DestinationFilename,
                    request.Needed,
                    Created = DateTime.UtcNow,
                    request.EnableDash,
                    request.EnableTwoPass, request.EnablePsnr
                });

            foreach (VideoDestinationFormat target in request.Targets)
            {
                connection.Execute(
                    "INSERT INTO FfmpegVideoRequestTargets (JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate, H264Level, H264Profile) VALUES(@JobCorrelationId, @Width, @Height, @VideoBitrate, @AudioBitrate, @Level, @Profile);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Width = target.Width,
                        Height = target.Height,
                        VideoBitrate = target.VideoBitrate,
                        AudioBitrate = target.AudioBitrate,
                        Level = target.Level,
                        Profile = target.Profile
                    });
            }

            int jobId = connection.ExecuteScalar<int>(
                        "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @JobState, @JobType);SELECT @@IDENTITY;",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Created = DateTimeOffset.UtcNow,
                            request.Needed,
                            JobState = TranscodingJobState.Queued,
                            JobType = JobType.Audio
                        });

            foreach (VideoTranscodingJob transcodingJob in jobs)
            {
                //var jobId = connection.ExecuteScalar<int>("INSERT INTO FfmpegVideoJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @VideoSourceFilename, @ChunkDuration, @State);SELECT @@IDENTITY;",
                //    new
                //    {
                //        JobCorrelationId = jobCorrelationId,
                //        Arguments = String.Join("|", transcodingJob.Arguments),
                //        Needed = transcodingJob.Needed,
                //        VideoSourceFilename = transcodingJob.SourceFilename,
                //        ChunkDuration = chunkDuration,
                //        State = transcodingJob.State
                //    });

                connection.Execute(
                    "INSERT INTO FfmpegTasks (FfmpegJobs_id, Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                    new
                    {
                        FfmpegJobsId = jobId,
                        transcodingJob.Arguments,
                        TaskState = TranscodingJobState.Queued,
                        DestinationFilename = string.Join(@"\n", transcodingJob.DestinationFilename),
                        transcodingJob.DestinationDurationSeconds,
                        VerifyOutput = true
                    });

                foreach (FfmpegPart part in transcodingJob.Chunks)
                {
                    VideoDestinationFormat format = request.Targets[part.Target];
                    connection.Execute(
                        "INSERT INTO FfmpegVideoParts (JobCorrelationId, Target, Filename, Number, FfmpegJobs_Id, Width, Height, Bitrate) VALUES(@JobCorrelationId, @Target, @Filename, @Number, @FfmpegVideoJobsId, @Width, @Height, @Bitrate);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Target = part.Target,
                            Filename = part.Filename,
                            Number = part.Number,
                            FfmpegVideoJobsId = jobId,
                            Width = format.Width,
                            Height = format.Height,
                            Bitrate = part.IsAudio ? format.AudioBitrate : format.VideoBitrate
                        });
                }
            }
        }
    }
}
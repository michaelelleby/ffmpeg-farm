using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using API.Service;
using Contract;
using Dapper;

namespace API.Repository
{
    public class VideoJobRepository : JobRepository, IVideoJobRepository
    {
        public VideoTranscodingJob GetNextTranscodingJob()
        {
            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            DateTime timeout =
                DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
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

                    var parts = connection.Query<dynamic>("SELECT Id, JobCorrelationId, Filename, Number, Target, PSNR FROM FfmpegVideoParts WHERE FfmpegVideoJobs_Id = @JobId;",
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
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
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
                        Arguments = new string[] { data.Arguments},
                        JobCorrelationId = data.JobCorrelationId
                    };
                }
            }
        }

        public Mp4boxJob GetNextDashJob()
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

        public void SaveJobs(JobRequest job, ICollection<VideoTranscodingJob> jobs, IDbConnection connection,
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
                    job.VideoSourceFilename,
                    job.AudioSourceFilename,
                    job.DestinationFilename,
                    job.Needed,
                    Created = DateTime.UtcNow,
                    job.EnableDash,
                    job.EnableTwoPass, job.EnablePsnr
                });

            foreach (DestinationFormat target in job.Targets)
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

            foreach (VideoTranscodingJob transcodingJob in jobs)
            {
                connection.Execute(
                    "INSERT INTO FfmpegVideoJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @VideoSourceFilename, @ChunkDuration, @State);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Arguments = String.Join("|", transcodingJob.Arguments),
                        Needed = transcodingJob.Needed,
                        VideoSourceFilename = transcodingJob.SourceFilename,
                        ChunkDuration = chunkDuration,
                        State = transcodingJob.State
                    });

                int jobId = connection.Query<int>("SELECT @@IDENTITY;")
                    .Single();

                foreach (FfmpegPart part in transcodingJob.Chunks)
                {
                    DestinationFormat format = job.Targets[part.Target];
                    connection.Execute(
                        "INSERT INTO FfmpegVideoParts (JobCorrelationId, Target, Filename, Number, FfmpegVideoJobs_Id, Width, Height, Bitrate) VALUES(@JobCorrelationId, @Target, @Filename, @Number, @FfmpegVideoJobsId, @Width, @Height, @Bitrate);",
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
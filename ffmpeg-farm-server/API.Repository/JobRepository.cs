using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Transactions;
using API.Service;
using Contract;
using Contract.Dto;
using Dapper;

namespace API.Repository
{
    public class JobRepository : IJobRepository
    {
        protected readonly IHelper Helper;

        public JobRepository(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            Helper = helper;
        }

        public bool DeleteJob(Guid jobId, JobType type)
        {
            using (var scope = new TransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    int rowsDeleted = -1;

                    switch (type)
                    {
                        case JobType.Audio:
                            rowsDeleted = DeleteAudioJob(jobId, connection);
                            break;
                        case JobType.Video:
                            rowsDeleted = DeleteVideoJob(jobId, connection);
                            break;
                        case JobType.VideoMp4box:
                            break;
                        case JobType.VideoMerge:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }

                    scope.Complete();

                    return rowsDeleted == 1;
                }
            }
        }

        public bool PauseJob(Guid jobId, JobType type)
        {
            using (var scope = new TransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    bool result = false;

                    switch (type)
                    {
                        case JobType.Audio:
                            result = PauseAudioJob(jobId, conn);
                            break;
                        case JobType.Video:
                            result = PauseVideoJob(jobId, conn);
                            break;
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException($"Job type {type} is not supported");
                    }

                    scope.Complete();

                    return result;
                }
            }
        }

        public bool ResumeJob(Guid jobId, JobType type)
        {
            using (var scope = new TransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    bool result = false;

                    switch (type)
                    {
                        case JobType.Audio:
                            result = ResumeAudioJob(jobId, conn);
                            break;
                        case JobType.Video:
                            result = ResumeVideoJob(jobId, conn);
                            break;
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException($"Job type {type} is not supported");
                    }

                    scope.Complete();

                    return result;
                }
            }
        }

        public IEnumerable<AudioJobRequestDto> Get()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                IDictionary<Guid, AudioJobRequestDto> requests = new ConcurrentDictionary<Guid, AudioJobRequestDto>();

                var rows = connection.Query<AudioJobRequestDto>(
                    "SELECT JobCorrelationId, SourceFilename, DestinationFilename, Needed, Created, OutputFolder FROM FfmpegAudioRequest ORDER BY Id ASC;");
                foreach (AudioJobRequestDto requestDto in rows)
                {
                    requests.Add(requestDto.JobCorrelationId, requestDto);
                }

                var jobs = connection.Query<AudioTranscodingJobDto>(
                    "SELECT Arguments, JobCorrelationId, Needed, SourceFilename, State, Started, Heartbeat, HeartbeatMachineName, Progress FROM FfmpegAudioJobs ORDER BY Id ASC;");

                foreach (AudioTranscodingJobDto dto in jobs)
                {
                    requests[dto.JobCorrelationId].Jobs.Add(dto);
                }

                return requests.Values;
            }
        }

        public IEnumerable<JobRequestDto> GetJobStatuses(Guid jobCorrelationId = default(Guid))
        {
            IEnumerable<TranscodingJobDto> jobs;
            IEnumerable<JobRequestDto> requests;
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                if (jobCorrelationId != default(Guid))
                {
                    requests =
                        connection.Query<JobRequestDto>("SELECT * from FfmpegAudioRequest WHERE JobCorrelationId = @JobCorrelationId;",
                                new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                    jobs =
                        connection.Query<TranscodingJobDto>(
                                "SELECT * FROM FfmpegAudioJobs WHERE JobCorrelationId = @JobCorrelationId;",
                                new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                }
                else
                {
                    requests = connection.Query<JobRequestDto>("SELECT * from FfmpegAudioRequest").ToList();
                    jobs = connection.Query<TranscodingJobDto>("SELECT * FROM FfmpegAudioJobs").ToList();
                }
            }

            return requests;
        }

        public void SaveProgress(BaseJob job)
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                int isVideo = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM FfmpegVideoRequest WHERE JobCorrelationId = @JobCorrelationId;",
                    new {job.JobCorrelationId});
                if (isVideo == 1)
                {
                    UpdateVideoJob(job, connection);
                    return;
                }

                int isAudio = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM FfmpegAudioRequest WHERE JobCorrelationId = @JobCorrelationId;",
                    new {job.JobCorrelationId});
                if (isAudio == 1)
                {
                    UpdateAudioJob(job, connection);
                }
            }
        }

        private static bool PauseVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @PausedState WHERE JobCorrelationId = @JobId AND State = @QueuedState",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            return rowsUpdated > 0;
        }

        private static bool PauseAudioJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegAudioJobs SET State = @PausedState WHERE JobCorrelationId = @JobId AND State = @QueuedState",
                new {JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued});

            return rowsUpdated > 0;
        }

        private static bool ResumeVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @QueuedState WHERE JobCorrelationId = @JobId AND State = @PausedState;",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            return rowsUpdated > 0;
        }

        private static bool ResumeAudioJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegAudioJobs SET State = @QueuedState WHERE JobCorrelationId = @JobId AND State = @PausedState;",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            return rowsUpdated > 0;
        }

        private static int DeleteAudioJob(Guid jobId, IDbConnection connection)
        {
            connection.Execute("DELETE FROM FfmpegAudioJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegAudioRequestTargets WHERE JobCorrelationId = @Id;", new {Id = jobId});

            int rowsDeleted = connection.Execute("DELETE FROM FfmpegAudioRequest WHERE JobCorrelationId = @Id;",
                new {Id = jobId});

            return rowsDeleted;
        }

        private static int DeleteVideoJob(Guid jobId, IDbConnection connection)
        {
            connection.Execute("DELETE FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegVideoParts WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegVideoRequestTargets WHERE JobCorrelationId = @Id;",
                new {Id = jobId});

            int rowsDeleted = connection.Execute("DELETE FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id;",
                new {Id = jobId});
            return rowsDeleted;
        }

        private static void UpdateAudioJob(BaseJob job, IDbConnection  connection)
        {
            using (var scope = new TransactionScope())
            {
                TranscodingJobState jobState = job.Failed
                    ? TranscodingJobState.Failed
                    : job.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                int updatedRows = connection.Execute(
                    "UPDATE FfmpegAudioJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
                    new
                    {
                        Id = job.Id,
                        Progress = job.Progress.TotalSeconds,
                        Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                        State = jobState,
                        MachineName = job.MachineName
                    });

                if (updatedRows != 1)
                    throw new Exception($"Failed to update progress for job id {job.Id}");

                scope.Complete();
            }
        }

        private static void UpdateVideoJob(BaseJob job, IDbConnection connection)
        {
            JobRequest jobRequest = connection.Query<JobRequest>(
                    "SELECT JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, EnableDash, EnablePsnr FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id",
                    new {Id = job.JobCorrelationId})
                .SingleOrDefault();
            if (jobRequest == null)
                throw new ArgumentException($@"Job with correlation id {job.JobCorrelationId} not found");

            jobRequest.Targets = connection.Query<DestinationFormat>(
                    "SELECT JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate FROM FfmpegVideoRequestTargets WHERE JobCorrelationId = @Id;",
                    new {Id = job.JobCorrelationId})
                .ToArray();

            using (var scope = new TransactionScope())
            {
                Type jobType = job.GetType();
                TranscodingJobState jobState = job.Failed
                    ? TranscodingJobState.Failed
                    : job.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                if (jobType == typeof(VideoTranscodingJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegVideoJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
                        new
                        {
                            Id = job.Id,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName,
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");

                    if (jobRequest.EnablePsnr)
                    {
                        foreach (FfmpegPart chunk in ((VideoTranscodingJob) job).Chunks)
                        {
                            connection.Execute(
                                "UPDATE FfmpegVideoParts SET PSNR = @Psnr WHERE Id = @Id;",
                                new {Id = chunk.Id, Psnr = chunk.Psnr});
                        }
                    }
                }
                else if (jobType == typeof(MergeJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegMergeJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
                        new
                        {
                            Id = job.Id,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");

                    var states = connection.Query<string>(
                            "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;",
                            new {Id = job.JobCorrelationId})
                        .Select(value => Enum.Parse(typeof(TranscodingJobState), value))
                        .Cast<TranscodingJobState>();

                    if (states.All(x => x == TranscodingJobState.Done))
                    {
                        string tempFolder = String.Concat(Path.GetDirectoryName(jobRequest.DestinationFilename),
                            Path.DirectorySeparatorChar, jobRequest.JobCorrelationId.ToString("N"));

                        Directory.Delete(tempFolder, true);
                    }
                }
                else if (jobType == typeof(Mp4boxJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE Mp4boxJobs SET Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE JobCorrelationId = @Id;",
                        new
                        {
                            Id = job.JobCorrelationId,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");
                }

                scope.Complete();
            }

            using (var scope = new TransactionScope())
            {
                ICollection<TranscodingJobState> totalJobs = connection.Query<TranscodingJobState>(
                        "SELECT State FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();

                if (totalJobs.Any(x => x != TranscodingJobState.Done))
                {
                    // Not all transcoding jobs are finished
                    return;
                }

                totalJobs = connection.Query<TranscodingJobState>(
                        "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();
                if (totalJobs.Any(x => x != TranscodingJobState.Done))
                {
                    // Not all merge jobs are finished
                    return;
                }

                string destinationFilename = jobRequest.DestinationFilename;
                string fileNameWithoutExtension =
                    Path.GetFileNameWithoutExtension(destinationFilename);
                string fileExtension = Path.GetExtension(destinationFilename);
                string outputFolder = Path.GetDirectoryName(destinationFilename);

                if (totalJobs.Count == 0)
                {
                    QueueMergeJob(job, connection, outputFolder, fileNameWithoutExtension, fileExtension, jobRequest);
                }
                else if (jobRequest.EnableDash)
                {
                    QueueMpegDashMergeJob(job, destinationFilename, connection, jobRequest, fileNameWithoutExtension,
                        outputFolder, fileExtension);
                }

                scope.Complete();
            }
        }

        private static void QueueMpegDashMergeJob(BaseJob job, string destinationFilename, IDbConnection connection,
            JobRequest jobRequest, string fileNameWithoutExtension, string outputFolder, string fileExtension)
        {
            string destinationFolder = Path.GetDirectoryName(destinationFilename);
            ICollection<TranscodingJobState> totalJobs =
                connection.Query<TranscodingJobState>("SELECT State FROM Mp4boxJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();

            // One MPEG DASH merge job is already queued. Do nothing
            if (totalJobs.Any())
                return;

            string arguments =
                $@"-dash 4000 -rap -frag-rap -profile onDemand -out {destinationFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}.mpd";

            var chunks = connection.Query<FfmpegPart>(
                "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegVideoParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                new {Id = job.JobCorrelationId});
            foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
            {
                int targetNumber = chunk.First().Target;
                DestinationFormat target = jobRequest.Targets[targetNumber];

                string targetFilename =
                    $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{target.Width}x{target
                        .Height}_{target.VideoBitrate}_{target.AudioBitrate}{fileExtension}";

                arguments += $@" {targetFilename}";
            }

            connection.Execute(
                "INSERT INTO Mp4boxJobs (JobCorrelationId, Arguments, Needed, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @State);",
                new
                {
                    JobCorrelationId = jobRequest.JobCorrelationId,
                    Arguments = arguments,
                    Needed = jobRequest.Needed,
                    State = TranscodingJobState.Queued
                });
        }

        private static void QueueMergeJob(BaseJob job, IDbConnection connection, string outputFolder,
            string fileNameWithoutExtension, string fileExtension, JobRequest jobRequest)
        {
            ICollection<FfmpegPart> chunks = connection.Query<FfmpegPart>(
                    "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename, Width, Height, Bitrate FROM FfmpegVideoParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                    new {Id = job.JobCorrelationId})
                .ToList();

            foreach (IEnumerable<FfmpegPart> chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
            {
                var FfmpegVideoParts = chunk as IList<FfmpegPart> ?? chunk.ToList();
                int targetNumber = FfmpegVideoParts.First().Target;
                DestinationFormat target = jobRequest.Targets[targetNumber];

                string targetFilename =
                    $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{target.Width}x{target.Height}_{target.VideoBitrate}_{target.AudioBitrate}{fileExtension}";

                // TODO Implement proper detection if files are already merged
                if (File.Exists(targetFilename))
                    continue;

                string path =
                    $"{outputFolder}{Path.DirectorySeparatorChar}{job.JobCorrelationId.ToString("N")}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{targetNumber}.list";

                using (TextWriter tw = new StreamWriter(path))
                {
                    foreach (FfmpegPart part in FfmpegVideoParts.Where(x => x.IsAudio == false))
                    {
                        tw.WriteLine($"file '{part.Filename}'");
                    }
                }
                string audioSource = chunks.Single(x => x.IsAudio && x.Bitrate == target.AudioBitrate).Filename;

                string arguments =
                    $@"-y -f concat -safe 0 -i ""{path}"" -i ""{audioSource}"" -c copy {targetFilename}";

                connection.Execute(
                    "INSERT INTO FfmpegMergeJobs (JobCorrelationId, Arguments, Needed, State, Target) VALUES(@JobCorrelationId, @Arguments, @Needed, @State, @Target);",
                    new
                    {
                        JobCorrelationId = job.JobCorrelationId,
                        Arguments = arguments,
                        Needed = jobRequest.Needed,
                        State = TranscodingJobState.Queued,
                        Target = targetNumber
                    });
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Transactions;
using API.Service;
using Contract;
using Dapper;

namespace API.Repository
{
    public class JobRepository : IJobRepository
    {
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

        public void PauseJob(Guid jobId, JobType type)
        {
            using (var scope = new TransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    switch (type)
                    {
                        case JobType.Audio:
                            PauseAudioJob(jobId, conn);
                            break;
                        case JobType.Video:
                            PauseVideoJob(jobId, conn);
                            break;
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException($"No job found with id {jobId:B}");
                    }

                    scope.Complete();
                }
            }
        }

        public void ResumeJob(Guid jobId, JobType type)
        {
            using (var scope = new TransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    switch (type)
                    {
                        case JobType.Audio:
                            ResumeAudioJob(jobId, conn);
                            break;
                        case JobType.Video:
                            ResumeVideoJob(jobId, conn);
                            break;
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException($"No job found with id {jobId:B}");
                    }

                    scope.Complete();
                }
            }
        }

        private static void PauseVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @PausedState WHERE JobCorrelationId = @JobId AND State = @QueuedState",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            if (rowsUpdated == 0)
                throw new InvalidOperationException($"Unable to pause any jobs for job {jobId:B} since none are in queued state and only jobs in queued state can be paused.");
        }

        private static void PauseAudioJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegAudioJobs SET State = @PausedState WHERE JobCorrelationId = @JobId AND State = @QueuedState",
                new {JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued});

            if (rowsUpdated == 0)
                throw new InvalidOperationException($"Unable to pause any jobs for job {jobId:B} since none are in queued state and only jobs in queued state can be paused.");
        }

        private static void ResumeVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @QueuedState WHERE JobCorrelationId = @JobId AND State = @PausedState;",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            if (rowsUpdated == 0)
                throw new InvalidOperationException($"Unable to resume any jobs for job {jobId:B} since none are in paused state and only jobs in paused state can be resumed.");
        }

        private static void ResumeAudioJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegAudioJobs SET State = @QueuedState WHERE JobCorrelationId = @JobId AND State = @PausedState;",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            if (rowsUpdated == 0)
                throw new InvalidOperationException($"Unable to resume any jobs for job {jobId:B} since none are in paused state and only jobs in paused state can be resumed.");
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
    }
}
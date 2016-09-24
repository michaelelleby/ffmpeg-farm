using System;
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
    }
}
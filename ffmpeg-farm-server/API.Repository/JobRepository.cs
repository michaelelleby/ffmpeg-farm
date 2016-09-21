using System;
using System.Transactions;
using API.Service;
using Contract;
using Dapper;

namespace API.Repository
{
    public class JobRepository : IJobRepository
    {
        public bool DeleteJob(Guid jobId)
        {
            using (var scope = new TransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    connection.Execute("DELETE FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegVideoParts WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegVideoRequestTargets WHERE JobCorrelationId = @Id;",
                        new {Id = jobId});

                    int rowsDeleted = connection.Execute("DELETE FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id;",
                        new {Id = jobId});

                    scope.Complete();

                    return rowsDeleted == 1;
                }
            }
        }
    }
}
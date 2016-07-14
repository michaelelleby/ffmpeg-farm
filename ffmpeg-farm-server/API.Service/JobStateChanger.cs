using System;
using System.Transactions;
using Contract;
using Dapper;

namespace API.Service
{
    public class JobStateChanger
    {
        public bool Change(Guid jobCorrelationId, TranscodingJobState newState, TranscodingJobState oldState)
        {
            if (jobCorrelationId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobCorrelationId}");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegJobs SET State = @NewState WHERE JobCorrelationId = @Id AND State = @OldState;",
                        new { Id = jobCorrelationId, Newstate = newState, OldState = oldState});

                    scope.Complete();

                    return rowsUpdated > 0;
                }
            }
        }
    }
}

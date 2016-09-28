using System;
using System.Transactions;
using Contract;
using Dapper;

namespace API.Service
{
    public class JobStateChanger
    {
        private readonly IHelper _helper;

        public JobStateChanger(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            _helper = helper;
        }

        public bool Change(Guid jobCorrelationId, TranscodingJobState newState, TranscodingJobState oldState)
        {
            if (jobCorrelationId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobCorrelationId}");

            using (var connection = _helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    

                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegVideoJobs SET State = @NewState WHERE JobCorrelationId = @Id AND State = @OldState;",
                        new { Id = jobCorrelationId, Newstate = newState, OldState = oldState});

                    scope.Complete();

                    return rowsUpdated > 0;
                }
            }
        }
    }
}

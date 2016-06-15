using System;
using System.Web.Http;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class PauseController : ApiController
    {
        /// <summary>
        /// Pause a job
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <returns>Number of tasks paused or zero if none were found in the queued state for the requested job</returns>
        [HttpPatch]
        public int Pause(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobId}");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegJobs SET State = @PausedState WHERE JobCorrelationId = ? AND State = @QueuedState;",
                        new {jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued});

                    transaction.Commit();

                    return rowsUpdated;
                }
            }
        }
    }
}

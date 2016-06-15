using System;
using System.Web.Http;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class ResumeController : ApiController
    {
        /// <summary>
        /// Resume a job
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <returns>Number of tasks set to active or zero if none were found in the queued state for the requested job</returns>
        [HttpPatch]
        public int Resume(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobId}");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegJobs SET State = @QueuedState WHERE JobCorrelationId = ? AND State = @PausedState;",
                        new { jobId, QueuedState = TranscodingJobState.Queued, PausedState = TranscodingJobState.Paused});

                    transaction.Commit();

                    return rowsUpdated;
                }
            }
        }
    }
}

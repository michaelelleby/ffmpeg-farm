using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
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
                        "UPDATE FfmpegJobs SET Active = 0 WHERE JobCorrelationId = ?;",
                        new { jobId });

                    transaction.Commit();

                    return rowsUpdated;
                }
            }
        }
    }
}

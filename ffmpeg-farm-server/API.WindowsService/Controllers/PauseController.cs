using System;
using System.Web.Http;
using API.Service;
using Contract;

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
        public bool Pause(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobId}");

            return new JobStateChanger().Change(jobId, TranscodingJobState.Paused, TranscodingJobState.Queued);
        }
    }
}

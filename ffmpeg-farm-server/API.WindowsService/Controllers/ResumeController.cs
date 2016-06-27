using System;
using System.Web.Http;
using API.Service;
using Contract;

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
        public bool Resume(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobId}");

            return new JobStateChanger().Change(jobId, TranscodingJobState.Queued, TranscodingJobState.Paused);
        }
    }
}

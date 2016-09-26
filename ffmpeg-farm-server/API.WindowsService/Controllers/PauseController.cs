using System;
using System.Web.Http;
using API.Service;
using Contract;

namespace API.WindowsService.Controllers
{
    public class PauseController : ApiController
    {
        private readonly IJobRepository _jobRepository;

        public PauseController(IJobRepository jobRepository)
        {
            if (jobRepository == null) throw new ArgumentNullException(nameof(jobRepository));

            _jobRepository = jobRepository;
        }
        
        /// <summary>
        /// Pause a job
        /// </summary>
        /// <param name="jobId">Job id</param>
        /// <returns>Number of tasks paused or zero if none were found in the queued state for the requested job</returns>
        [HttpPatch]
        public void Pause(Guid jobId, JobType type)
        {
            if (jobId == Guid.Empty) throw new ArgumentException($@"Invalid Job Id specified: {jobId}");
            if (type == JobType.Unknown) throw new ArgumentOutOfRangeException(nameof(type));

            _jobRepository.PauseJob(jobId, type);
        }
    }
}

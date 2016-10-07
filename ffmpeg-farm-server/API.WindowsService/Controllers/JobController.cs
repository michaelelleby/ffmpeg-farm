using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Contract.Dto;

namespace API.WindowsService.Controllers
{
    public class JobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly IJobRepository _repository;

        public JobController(IHelper helper, IJobRepository repository)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _helper = helper;
            _repository = repository;
        }

        [HttpGet]
        public FFmpegTaskDto GetNext(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified"));
            }

            _helper.InsertClientHeartbeat(machineName);

            return _repository.GetNextJob();
        }

        [HttpDelete]
        public bool DeleteJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Specified jobCorrelationId is invalid"));
            }

            return _repository.DeleteJob(jobCorrelationId);
        }

        [HttpPatch]
        [Route("api/job/pause/{jobCorrelationId}")]
        public bool PauseJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Specified jobCorrelationId is invalid"));
            }

            return _repository.PauseJob(jobCorrelationId);
        }

        [HttpPatch]
        [Route("api/job/resume/{jobCorrelationId}")]
        public bool ResumeJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Specified jobCorrelationId is invalid"));
            }

            return _repository.ResumeJob(jobCorrelationId);
        }
    }
}
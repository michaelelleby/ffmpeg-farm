using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;
using Contract.Dto;

namespace API.WindowsService.Controllers
{
    public class JobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly IJobRepository _repository;
        private readonly ILogging _logging;

        public JobController(IHelper helper, IJobRepository repository, ILogging logging)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _helper = helper;
            _repository = repository;
            _logging = logging;
        }

        [HttpDelete]
        public bool DeleteJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), "Specified jobCorrelationId is invalid");

            var res = _repository.DeleteJob(jobCorrelationId);
            if (res)
                _logging.Info($"Job {jobCorrelationId} deleted");
            else 
                _logging.Warn($"Failed to delete job {jobCorrelationId}");
            return res;
        }

        [HttpPatch]
        public bool PatchJob(Guid jobCorrelationId, Command command)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), "Specified jobCorrelationId is invalid");

            bool res;

            switch (command)
            {
                case Command.Pause:
                    res = _repository.PauseJob(jobCorrelationId);
                    break;
                case Command.Resume:
                    res = _repository.ResumeJob(jobCorrelationId);
                    break;
                case Command.Cancel:
                    res = _repository.CancelJob(jobCorrelationId);
                    break;
                case Command.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), $"unsupported {command}", nameof(command));
            }
            if (res)
                _logging.Info($"Recived valid {command} order for job {jobCorrelationId}.");
            else
                _logging.Warn($"Recived invalid {command} order for job {jobCorrelationId}.");
            return res;
        }
    }
}
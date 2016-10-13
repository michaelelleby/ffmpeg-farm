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

        public JobController(IHelper helper, IJobRepository repository)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _helper = helper;
            _repository = repository;
        }

        [HttpDelete]
        public bool DeleteJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), "Specified jobCorrelationId is invalid");

            return _repository.DeleteJob(jobCorrelationId);
        }

        [HttpPatch]
        public bool PatchJob(Guid jobCorrelationId, Command command)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), "Specified jobCorrelationId is invalid");


            switch (command)
            {
                case Command.Pause:
                    return _repository.PauseJob(jobCorrelationId);
                case Command.Resume:
                    return _repository.ResumeJob(jobCorrelationId);
                case Command.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), $"unsupported {command}", nameof(command));
            }
        }
    }
}
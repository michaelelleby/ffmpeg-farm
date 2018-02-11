using System;
using System.Linq;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class JobController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public JobController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        [HttpDelete]
        public bool DeleteJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId),
                    @"Specified jobCorrelationId is invalid");
            var job = _unitOfWork.Jobs.Find(x => x.JobCorrelationId == jobCorrelationId)
                .FirstOrDefault();

            if (job == null)
                return false;

            bool removed = _unitOfWork.Jobs.Remove(job);
            if (removed)
            {
                _unitOfWork.Complete();
            }

            return removed;
        }

        [HttpPatch]
        public bool PatchJob(Guid jobCorrelationId, Command command)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId),
                    @"Specified jobCorrelationId is invalid");

            bool res;
            switch (command)
            {
                case Command.Pause:
                    res = _unitOfWork.Jobs.PauseJob(jobCorrelationId);
                    break;
                case Command.Resume:
                    res = _unitOfWork.Jobs.ResumeJob(jobCorrelationId);
                    break;
                case Command.Cancel:
                    res = _unitOfWork.Jobs.CancelJob(jobCorrelationId);
                    break;
                case Command.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), $"unsupported {command}", nameof(command));
            }

            if (res)
            {
                _unitOfWork.Complete();
            }

            return res;
        }
    }
}
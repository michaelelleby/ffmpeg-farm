using System;
using System.Linq;
using System.Web.Http;
using API.Database;
using API.Repository;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class JobController : ApiController
    {
        [HttpDelete]
        public bool DeleteJob(Guid jobCorrelationId)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), "Specified jobCorrelationId is invalid");

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                var job = unitOfWork.Jobs.Find(x => x.JobCorrelationId == jobCorrelationId)
                    .FirstOrDefault();

                if (job == null)
                    return false;

                unitOfWork.Jobs.Remove(job);

                unitOfWork.Complete();

                return true;
            }
        }

        [HttpPatch]
        public bool PatchJob(Guid jobCorrelationId, Command command)
        {
            if (jobCorrelationId == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(jobCorrelationId), @"Specified jobCorrelationId is invalid");

            bool res;

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                switch (command)
                {
                    case Command.Pause:
                        res = unitOfWork.Jobs.PauseJob(jobCorrelationId);
                        break;
                    case Command.Resume:
                        res = unitOfWork.Jobs.ResumeJob(jobCorrelationId);
                        break;
                    case Command.Cancel:
                        res = unitOfWork.Jobs.CancelJob(jobCorrelationId);
                        break;
                    case Command.Unknown:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), $"unsupported {command}", nameof(command));
                }

                if (res)
                {
                    unitOfWork.Complete();
                }
            }

            return res;
        }
    }
}
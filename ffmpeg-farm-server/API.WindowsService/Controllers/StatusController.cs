using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Database;
using API.Repository;
using API.WindowsService.Models;
using Contract;
using Contract.Models;
using WebApi.OutputCache.V2;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        /// <summary>
        /// Get status for all jobs
        /// </summary>
        /// <returns></returns>
        [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [HttpGet]
        public IEnumerable<FfmpegJobModel> Get(int take = 10)
        {
            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                return unitOfWork.Jobs.GetAll().Take(take)
                    .Select(MapDtoToModel);
            }
        }

        /// <summary>
        /// Get status for a specific job
        /// </summary>
        /// <param name="id">ID of job to get status of</param>
        /// <returns></returns>
        [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [HttpGet]
        public FfmpegJobModel Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(id), "ID must be a valid GUID");

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                var dto = unitOfWork.Jobs.Find(j => j.JobCorrelationId == id)
                    .FirstOrDefault();
                if (dto == null)
                    return null;

                return new FfmpegJobModel
                {
                    JobCorrelationId = id,
                    Created = dto.Created,
                    Needed = dto.Needed
                };
            }
        }

        /// <summary>
        /// Update progress of an active job.
        /// 
        /// This also serves as a heartbeat, to tell the server
        /// that the client is still working actively on the job
        /// </summary>
        /// <param name="model"></param>
        [HttpPatch]
        public TranscodingJobState UpdateProgress(TaskProgressModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                var task = unitOfWork.Tasks.Get(model.Id);
                if (task == null)
                    throw new ArgumentOutOfRangeException(nameof(model), $"No task found with id {model.Id}");

                TranscodingJobState jobState = model.Failed
                    ? TranscodingJobState.Failed
                    : model.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                task.TaskState = jobState;
                task.Progress = model.Progress.TotalSeconds;
                task.VerifyProgress = model.VerifyProgress?.TotalSeconds;
                task.Heartbeat = DateTimeOffset.UtcNow;
                task.HeartbeatMachineName = model.MachineName;

                unitOfWork.Complete();

                return task.TaskState;
            }
        }

        private static FfmpegJobModel MapDtoToModel(FfmpegJobs job)
        {
            return new FfmpegJobModel
            {
                JobCorrelationId = job.JobCorrelationId,
                Needed = job.Needed,
                Created = job.Created,
                Tasks = job.FfmpegTasks.Select(j => new FfmpegTaskModel
                {
                    Started = j.Started,
                    Heartbeat = j.Heartbeat,
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.TaskState,
                    Progress = Math.Round(Convert.ToDecimal(j.Progress / j.DestinationDurationSeconds * 100), 2, MidpointRounding.ToEven),
                    VerifyProgres = j.VerifyProgress == null ? (decimal?) null : Math.Round(Convert.ToDecimal(j.VerifyProgress.Value / j.DestinationDurationSeconds * 100), 2, MidpointRounding.ToEven),
                    DestinationFilename = j.DestinationFilename,
                })
            };
        }
    }
}


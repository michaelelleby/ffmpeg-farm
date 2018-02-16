using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Database;
using API.WindowsService.Models;
using Contract;
using Contract.Models;

namespace API.WindowsService.Controllers
{
    [RoutePrefix("api/status")]
    public class StatusController : ApiController
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatusController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        ///     Get status for all jobs
        /// </summary>
        [Route("{take:int}")]
        [HttpGet]
        public IEnumerable<FfmpegJobModel> Get(int take = 10)
        {
            return _unitOfWork.Jobs.GetAll().Take(take)
                .Select(MapDtoToModel);
        }

        /// <summary>
        ///     Get status for a specific job
        /// </summary>
        /// <param name="id">ID of job to get status of</param>
        [Route("{id:guid}")]
        [HttpGet]
        public FfmpegJobModel Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(id), @"ID must be a valid GUID");

            var dto = _unitOfWork.Jobs.Find(j => j.JobCorrelationId == id)
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

        /// <summary>
        ///     Update progress of an active job.
        ///     This also serves as a heartbeat, to tell the server
        ///     that the client is still working actively on the job
        /// </summary>
        [Route]
        [HttpPatch]
        public TranscodingJobState UpdateProgress(TaskProgressModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!ModelState.IsValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

            var jobState = model.Failed
                ? TranscodingJobState.Failed
                : model.Done
                    ? TranscodingJobState.Done
                    : TranscodingJobState.InProgress;

            var task = _unitOfWork.Tasks.Get(model.Id);
            if (task == null)
                throw new ArgumentOutOfRangeException(nameof(model), $@"No task found with id {model.Id}");

            task.TaskState = jobState;
            task.Progress = model.Progress.TotalSeconds;
            task.VerifyProgress = model.VerifyProgress?.TotalSeconds;
            task.Heartbeat = DateTimeOffset.UtcNow;
            task.HeartbeatMachineName = model.MachineName;

            try
            {
                _unitOfWork.Complete();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // The current TaskState might be different from what we just tried to set it to, so reload from database to get the current state
                ex.Entries.Single().Reload();
            }

            return task.TaskState;
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
                    DestinationFilename = j.DestinationFilename
                })
            };
        }
    }
}
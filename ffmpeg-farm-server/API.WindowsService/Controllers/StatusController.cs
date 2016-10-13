using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;
using Contract.Dto;
using Contract.Models;
using FfmpegTaskModel = Contract.Models.FfmpegTaskModel;
using JobStatus = Contract.Models.JobStatus;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IJobRepository _repository;
        private readonly IHelper _helper;

        public StatusController(IJobRepository repository, IHelper helper)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _repository = repository;
            _helper = helper;
        }

        /// <summary>
        /// Get status for all jobs
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FfmpegJobModel> Get()
        {
            ICollection<FFmpegJobDto> jobStatuses = _repository.Get();
            IEnumerable<FfmpegJobModel> requestModels = jobStatuses.Select(m => new FfmpegJobModel
            {
                JobCorrelationId = m.JobCorrelationId,
                Needed = m.Needed,
                Created = m.Created,
                Tasks = m.Tasks.Select(j => new FfmpegTaskModel
                {
                    Heartbeat = j.Heartbeat,
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.State,
                })
            });

            return requestModels;
        }

        /// <summary>
        /// Get status for a specific job
        /// </summary>
        /// <param name="id">ID of job to get status of</param>
        /// <returns></returns>
        public JobStatus Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(id), "ID must be a valid GUID");

            FFmpegJobDto job = _repository.Get(id);
            if (job == null)
                throw new ArgumentException($"No job found with id {id:B}", "id");

            JobStatus result = new JobStatus
            {
                State = GetJobStatus(job),
                JobCorrelationId = job.JobCorrelationId,
                Created = job.Created
            };

            if (result.State == TranscodingJobState.Done)
            {
                result.OutputFiles = GetOutputFiles(job.Tasks);
            }

            return result;
        }

        private static ICollection<string> GetOutputFiles(ICollection<FFmpegTaskDto> tasks)
        {
            return tasks.Select(task => task.DestinationFilename)
                .ToList();
        }

        private static TranscodingJobState GetJobStatus(FFmpegJobDto job)
        {
            var transcodingJobs = job.Tasks.Select(j => new Models.FfmpegTaskModel
            {
                Heartbeat = j.Heartbeat.GetValueOrDefault(),
                HeartbeatMachine = j.HeartbeatMachineName,
                State = j.State
            });

            if (transcodingJobs.All(x => x.State == TranscodingJobState.Done))
                return TranscodingJobState.Done;
  
            if (transcodingJobs.Any(j => j.State == TranscodingJobState.Failed))
                return TranscodingJobState.Failed;

            if (transcodingJobs.Any(j => j.State == TranscodingJobState.Paused))
                return TranscodingJobState.Paused;

            if (transcodingJobs.Any(j => j.State == TranscodingJobState.InProgress))
                return TranscodingJobState.InProgress;

            if (transcodingJobs.Any(j => j.State == TranscodingJobState.Queued))
                return TranscodingJobState.Queued;

            return TranscodingJobState.Unknown;
        }

        /// <summary>
        /// Update progress of an active job.
        /// 
        /// This also serves as a heartbeat, to tell the server
        /// that the client is still working actively on the job
        /// </summary>
        /// <param name="model"></param>
        [HttpPatch]
        public void UpdateProgress(TaskProgressModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            _helper.InsertClientHeartbeat(model.MachineName);

            _repository.SaveProgress(model.Id, model.Failed, model.Done, model.Progress, model.MachineName);
        }
    }
}


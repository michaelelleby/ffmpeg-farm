using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using API.Repository;
using API.Service;
using Contract;
using Contract.Dto;
using Contract.Models;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IAudioJobRepository _repository;
        private readonly IHelper _helper;

        public StatusController(IAudioJobRepository repository, IHelper helper)
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
        public IEnumerable<JobRequestModel> Get()
        {
            IEnumerable<AudioJobRequestDto> jobStatuses = _repository.Get();
            IEnumerable<JobRequestModel> requestModels = jobStatuses.Select(m => new JobRequestModel
            {
                JobCorrelationId = m.JobCorrelationId,
                SourceFilename = m.SourceFilename,
                DestinationFilenamePrefix = m.DestinationFilename,
                Needed = m.Needed.GetValueOrDefault(),
                Created = m.Created,
                Jobs = m.Jobs.Select(j => new TranscodingJobModel
                {
                    Heartbeat = j.Heartbeat.GetValueOrDefault(),
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
        public JobRequestModel Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "ID must be a valid GUID"));

            AudioJobRequestDto request = _repository.Get(id);
            if (request == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No job found with id {id:B}"));

            JobRequestModel model = new JobRequestModel
            {
                JobCorrelationId = request.JobCorrelationId,
                SourceFilename = request.SourceFilename,
                DestinationFilenamePrefix = request.DestinationFilename,
                Needed = request.Needed.GetValueOrDefault(),
                Created = request.Created,
                OutputFolder = request.OutputFolder,
                Jobs = request.Jobs.Select(j => new TranscodingJobModel
                {
                    Heartbeat = j.Heartbeat.GetValueOrDefault(),
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.State
                })
            };


            return model;
        }

        /// <summary>
        /// Update progress of an active job.
        /// 
        /// This also serves as a heartbeat, to tell the server
        /// that the client is still working actively on the job
        /// </summary>
        /// <param name="job"></param>
        [HttpPut]
        public void UpdateProgress(BaseJob job)
        {
            if (job == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Job parameter must not be empty"));
            if (string.IsNullOrWhiteSpace(job.MachineName))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified"));

            _helper.InsertClientHeartbeat(job.MachineName);

            _repository.SaveProgress(job);
        }
    }
}

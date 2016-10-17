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
//using FfmpegTaskModel = Contract.Models.FfmpegTaskModel;

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
            IEnumerable<FfmpegJobModel> requestModels = jobStatuses.Select(MapDtoToModel);

            return requestModels;
        }

        
        /// <summary>
        /// Get status for a specific job
        /// </summary>
        /// <param name="id">ID of job to get status of</param>
        /// <returns></returns>
        public FfmpegJobModel Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(id), "ID must be a valid GUID");

            FFmpegJobDto job = _repository.Get(id);

            if (job == null)
                throw new ArgumentException($"No job found with id {id:B}", "id");

            var result = MapDtoToModel(job);

            return result;
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

            _repository.SaveProgress(model.Id, model.Failed, model.Done, model.Progress, model.MachineName);
        }

        private static FfmpegJobModel MapDtoToModel(FFmpegJobDto dto)
        {
            return new FfmpegJobModel
            {
                JobCorrelationId = dto.JobCorrelationId,
                Needed = dto.Needed,
                Created = dto.Created,
                Tasks = dto.Tasks.Select(j => new FfmpegTaskModel
                {
                    Heartbeat = j.Heartbeat,
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.State,
                    Progress = Math.Round(Convert.ToDecimal(j.Progress / j.DestinationDurationSeconds * 100), 2, MidpointRounding.ToEven),
                    DestinationFilename = j.DestinationFilename
                }),
            };
        }
    }
}


using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;
using Contract.Dto;
using Contract.Models;
using WebApi.OutputCache.V2;

//using FfmpegTaskModel = Contract.Models.FfmpegTaskModel;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;
        private static readonly string _logPath = ConfigurationManager.AppSettings["FFmpegLogPath"];

        public StatusController(IJobRepository repository, IHelper helper, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _helper = helper;
            _logging = logging;
        }

        /// <summary>
        /// Get status for all jobs
        /// </summary>
        /// <returns></returns>
        [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [HttpGet]
        public IEnumerable<FfmpegJobModel> Get(int take = 10)
        {
            ICollection<FFmpegJobDto> jobStatuses = _repository.Get(take);
            if (jobStatuses.Count > 0)
            {
                return jobStatuses.Select(MapDtoToModel);
            }

            return new List<FfmpegJobModel>();
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
        public TranscodingJobState UpdateProgress(TaskProgressModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }
            if (model.Failed)
                _logging.Warn($"Task {model.Id} failed at {model.MachineName}");
            if (model.Done)
                _logging.Info($"Task {model.Id} done at {model.MachineName}");
            return _repository.SaveProgress(model.Id, model.Failed, model.Done, model.Progress, model.VerifyProgress, model.MachineName);
        }

        private static FfmpegJobModel MapDtoToModel(FFmpegJobDto dto)
        {
            return new FfmpegJobModel
            {
                JobCorrelationId = dto.JobCorrelationId,
                Needed = dto.Needed,
                Created = dto.Created,
                Tasks = dto.Tasks.Select(j =>
                {
                    var progress = CalculateProgress(j);
                    return new FfmpegTaskModel
                    {
                        Started = j.Started,
                        Heartbeat = j.Heartbeat,
                        HeartbeatMachine = j.HeartbeatMachineName,
                        State = j.State,
                        Progress = progress,
                        VerifyProgres = j.VerifyProgress == null ? (decimal?) null : progress,
                        DestinationFilename = j.DestinationFilename,
                        LogPath = j.Started != null
                            ? $@"{_logPath}{j.Started.Value.Date:yyyy\\MM\\dd}\task_{j.Id}_output.txt"
                            : string.Empty
                    };
                }),
            };
        }

        private static decimal CalculateProgress(FFmpegTaskDto j)
        {
            return Math.Round(Convert.ToDecimal(NotZero((int?)j.VerifyProgress) / NotZero(j.DestinationDurationSeconds * 100)), 2, MidpointRounding.ToEven);
        }

        private static int NotZero(int? number)
        {
            if (number.HasValue && number != 0)
                return number.Value;
            return 1;
        }
    }
}


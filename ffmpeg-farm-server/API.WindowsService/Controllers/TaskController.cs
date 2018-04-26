using System;
using System.Web.Http;
using Contract;
using Contract.Dto;
using WebApi.OutputCache.V2;

namespace API.WindowsService.Controllers
{
    public class TaskController : ApiController
    {
        private readonly IJobRepository _repository;
        private readonly ILogging _logging;

        public TaskController(IJobRepository repository, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _logging = logging;
        }

        /// <summary>
        /// Get next waiting task.
        /// </summary>
        /// <param name="machineName">Caller-id</param>
        [HttpGet]
        [CacheOutput(NoCache = true)]
        public FFmpegTaskDto GetNext(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
                throw new ArgumentNullException(nameof(machineName));

            var res = _repository.GetNextJob(machineName);
            if (res != null)
            {
                var guid = _repository.GetGuidById(res.FfmpegJobsId);
                _logging.Debug($"Assigned task {res.Id} (job id {res.FfmpegJobsId}) to {machineName}. Job correlation id {guid}");
            }
            return res;
        }
    }
}

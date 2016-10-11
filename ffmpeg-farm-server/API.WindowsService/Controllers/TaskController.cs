using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Contract.Dto;

namespace API.WindowsService.Controllers
{
    public class TaskController : ApiController
    {
        private readonly IHelper _helper;
        private readonly IJobRepository _repository;

        public TaskController(IHelper helper, IJobRepository repository)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _helper = helper;
            _repository = repository;
        }

        /// <summary>
        /// Get next waiting task.
        /// </summary>
        /// <param name="machineName">Caller-id</param>
        [HttpGet]
        public FFmpegTaskDto GetNext(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified"));
            }

            _helper.InsertClientHeartbeat(machineName);

            return _repository.GetNextJob();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;
using WebApi.OutputCache.V2;

namespace API.WindowsService.Controllers
{
    public class ClientController : ApiController
    {
        private readonly IHelper _helper;
        private readonly IJobRepository _jobRepository;

        public ClientController(IHelper helper, IJobRepository jobRepository)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (jobRepository ==null ) throw new ArgumentNullException(nameof(jobRepository));
            _helper = helper;
            _jobRepository = jobRepository;
        }

        [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [HttpGet]
        public IEnumerable<ClientHeartbeat> Get()
        {
            using (var connection = _helper.GetConnection())
            {
                connection.Open();

                return connection.Query<ClientHeartbeat>("SELECT MachineName, LastHeartbeat FROM Clients;");
            }
        }


        /// <summary>
        /// Removes client which hasn't send an heart in the last hour. Admin use only, do NOT use unless you know what you are doing.
        /// </summary>
        /// <param name="maxAge">client max age in TimeSpan format, default value one hour.</param> 
        /// <returns>Number of clients removed.</returns>
        [HttpPost]
        [Route("~/admin/PruneClients")]
        
        public int PruneInactiveClients(TimeSpan? maxAge = null)
        {
            var maxAgeValue = maxAge ?? TimeSpan.FromHours(1);
            return _jobRepository.PruneInactiveClients(maxAgeValue);
        }
    }
}

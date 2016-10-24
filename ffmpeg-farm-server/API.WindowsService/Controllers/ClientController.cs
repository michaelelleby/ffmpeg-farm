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

        public ClientController(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _helper = helper;
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
    }
}

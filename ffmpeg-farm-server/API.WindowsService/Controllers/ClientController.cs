using System;
using System.Collections.Generic;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;

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

using System.Collections.Generic;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class ClientController : ApiController
    {
        public IEnumerable<ClientHeartbeat> Get()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                return connection.Query<ClientHeartbeat>("SELECT MachineName, LastHeartbeat FROM Clients;");
            }
        }
    }
}

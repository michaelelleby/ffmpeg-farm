using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Dapper;

namespace ffmpeg_farm_server.Controllers
{
    public class ClientController : ApiController
    {
        public IEnumerable<ClientHeartbeat> Get()
        {
            using (var connection = Helper.GetConnection())
            {
                return connection.Query<ClientHeartbeat>("SELECT MachineName, LastHeartbeat FROM Clients;");
            }
        }
    }
}

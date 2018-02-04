using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using API.Database;
using API.Repository;
using API.Service;
using Contract;
using Dapper;
using WebApi.OutputCache.V2;

namespace API.WindowsService.Controllers
{
    public class ClientController : ApiController
    {
        [CacheOutput(ClientTimeSpan = 5, ServerTimeSpan = 5)]
        [HttpGet]
        public IEnumerable<ClientHeartbeat> Get()
        {
            IEnumerable<Clients> clients;
            using (IUnitOfWork unitofwork = new UnitOfWork(new FfmpegFarmContext()))
            {
                clients = unitofwork.Clients.GetAll();
            }

            return clients.Select(c => new ClientHeartbeat {LastHeartbeat = c.LastHeartbeat, MachineName = c.MachineName});
        }

        ///// <summary>
        ///// Removes client which hasn't send an heart in the last hour. Admin use only, do NOT use unless you know what you are doing.
        ///// </summary>
        ///// <param name="maxAge">client max age in TimeSpan format, default value one hour.</param> 
        ///// <returns>Number of clients removed.</returns>
        //[HttpPost]
        //[Route("~/admin/PruneClients")]
        
        //public int PruneInactiveClients(TimeSpan? maxAge = null)
        //{
        //    var maxAgeValue = maxAge ?? TimeSpan.FromHours(1);
        //    return _IOldJobRepository.PruneInactiveClients(maxAgeValue);
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using API.Database;
using API.Repository;
using Contract;

namespace API.WindowsService.Controllers
{
    [RoutePrefix("api/client")]
    public class ClientController : ApiController
    {
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
            DateTimeOffset timeout = DateTimeOffset.UtcNow.Subtract(maxAgeValue);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                int clientsRemoved = unitOfWork.Clients.PruneInactiveClients(timeout);

                unitOfWork.Complete();

                return clientsRemoved;
            }
        }
    }
}
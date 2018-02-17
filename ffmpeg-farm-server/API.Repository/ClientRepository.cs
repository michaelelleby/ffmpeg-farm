using System;
using System.Data.Entity;
using System.Linq;
using API.Database;
using Contract;

namespace API.Repository
{
    public class ClientRepository : BaseRepository<Clients>, IClientRepository
    {
        public ClientRepository(FfmpegFarmContext context) : base(context)
        {
        }

        public int PruneInactiveClients(DateTimeOffset timeout)
        {
            var set = Context.Set<Clients>();
            var inactiveclients = set.Where(x => x.LastHeartbeat < timeout);

            return set.RemoveRange(inactiveclients).Count();
        }
    }
}
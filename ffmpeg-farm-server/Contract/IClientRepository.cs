using System;
using API.Database;

namespace Contract
{
    public interface IClientRepository : IRepository<Clients>
    {
        int PruneInactiveClients(DateTimeOffset timeout);
    }
}
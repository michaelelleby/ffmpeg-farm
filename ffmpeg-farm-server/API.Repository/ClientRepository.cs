using System.Data.Entity;
using API.Database;
using Contract;

namespace API.Repository
{
    public class ClientRepository : BaseRepository<Clients>, IClientRepository
    {
        public ClientRepository(FfmpegFarmContext context) : base(context)
        {
        }
    }
}
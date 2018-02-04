using System.Data.Entity;
using API.Database;
using Contract;

namespace API.Repository
{
    public class JobRepository : BaseRepository<FfmpegJobs>, IJobRepository
    {
        public JobRepository(FfmpegFarmContext context) : base(context)
        {
        }
    }
}
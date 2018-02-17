using System.Data.Entity;
using Contract;

namespace API.Repository
{
    public class HardSubtitlesRequestRepository : BaseRepository<HardSubtitlesJobRequest>, IHardSubtitlesRequestRepository
    {
        public HardSubtitlesRequestRepository(DbContext context) : base(context)
        {
        }
    }
}
using System.Data.Entity;
using API.Database;
using Contract;

namespace API.Repository
{
    public class MuxJobRepository : BaseRepository<FfmpegMuxRequest>, IMuxJobRepository
    {
        public MuxJobRepository(DbContext context) : base(context)
        {
        }
    }
}
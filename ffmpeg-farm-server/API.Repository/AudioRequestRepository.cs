using System;
using API.Database;
using Contract;

namespace API.Repository
{
    public class AudioRequestRepository : BaseRepository<FfmpegAudioRequest>, IAudioRequestRepository

    {
        public AudioRequestRepository(FfmpegFarmContext context) : base(context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
        }
    }
}
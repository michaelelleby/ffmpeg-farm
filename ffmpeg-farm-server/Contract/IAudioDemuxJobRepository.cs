using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IAudioDemuxJobRepository : IJobRepository
    {
        Guid Add(AudioDemuxJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IAudioDemuxJobRepository : IOldJobRepository
    {
        Guid Add(AudioDemuxJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
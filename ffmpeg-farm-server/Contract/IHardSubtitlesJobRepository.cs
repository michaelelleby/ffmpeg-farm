using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IHardSubtitlesJobRepository : IOldJobRepository
    {
        Guid Add(HardSubtitlesJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
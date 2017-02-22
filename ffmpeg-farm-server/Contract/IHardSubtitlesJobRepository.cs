using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IHardSubtitlesJobRepository : IJobRepository
    {
        Guid Add(HardSubtitlesJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
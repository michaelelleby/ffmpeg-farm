using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IHardSubtilesJobRepository : IJobRepository
    {
        Guid Add(HardSubtilesJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IMuxJobRepository : IJobRepository
    {
        Guid Add(MuxJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
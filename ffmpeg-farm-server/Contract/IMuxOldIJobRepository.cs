using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IMuxIOldJobRepository : IOldJobRepository
    {
        Guid Add(MuxJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
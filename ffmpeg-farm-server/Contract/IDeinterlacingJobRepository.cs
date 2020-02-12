using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IDeinterlacingJobRepository
    {
        Guid Add(DeinterlacingJobRequest request, ICollection<FFmpegJob> jobs);
    }
}
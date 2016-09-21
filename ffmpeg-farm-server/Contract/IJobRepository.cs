using System;

namespace Contract
{
    public interface IJobRepository
    {
        bool DeleteJob(Guid jobId);
    }
}
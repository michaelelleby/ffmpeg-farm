using System;

namespace Contract
{
    public interface IJobRepository
    {
        bool DeleteJob(Guid jobId, JobType type);
        void PauseJob(Guid jobId, JobType type);
        void ResumeJob(Guid jobId, JobType type);
    }
}
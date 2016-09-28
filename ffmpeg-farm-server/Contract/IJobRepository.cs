using System;

namespace Contract
{
    public interface IJobRepository
    {
        bool DeleteJob(Guid jobId, JobType type);
        bool PauseJob(Guid jobId, JobType type);
        bool ResumeJob(Guid jobId, JobType type);
        void SaveProgress(BaseJob job);
    }
}
using System;
using API.Database;

namespace Contract
{
    public interface IJobRepository : IRepository<FfmpegJobs>
    {
        bool PauseJob(Guid jobCorrelationId);
        bool ResumeJob(Guid jobCorrelationId);
        bool CancelJob(Guid jobCorrelationId);
    }
}
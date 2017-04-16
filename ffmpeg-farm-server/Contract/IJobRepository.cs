using System;
using System.Collections.Generic;
using Contract.Dto;

namespace Contract
{
    public interface IJobRepository
    {
        bool DeleteJob(Guid jobId);
        bool PauseJob(Guid jobId);
        bool ResumeJob(Guid jobId);
        bool CancelJob(Guid jobId);
        bool SaveProgress(int jobId, bool failed, bool done, TimeSpan progress, TimeSpan? verifyProgress, string machineName, DateTimeOffset timestamp);
        FFmpegTaskDto GetNextJob(string machineName);
        FFmpegJobDto Get(Guid id);
        ICollection<FFmpegJobDto> Get(int take = 10);
        Guid GetGuidById(int id);

        int PruneInactiveClients(TimeSpan maxAge);
    }
}
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
        void SaveProgress(int jobId, bool failed, bool done, TimeSpan progress, string machineName);
        FFmpegTaskDto GetNextJob(string machineName);
        FFmpegJobDto Get(Guid id);
        ICollection<FFmpegJobDto> Get(int take = 10);
    }
}
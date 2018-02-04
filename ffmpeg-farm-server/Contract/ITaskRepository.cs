using System;
using API.Database;

namespace Contract
{
    public interface ITaskRepository : IRepository<FfmpegTasks>
    {
        FfmpegTasks GetNext(TimeSpan timeout);
    }
}
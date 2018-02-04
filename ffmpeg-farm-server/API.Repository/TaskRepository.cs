using System;
using System.Data.Entity;
using System.Linq;
using API.Database;
using Contract;

namespace API.Repository
{
    public class TaskRepository : BaseRepository<FfmpegTasks>, ITaskRepository
    {
        public TaskRepository(FfmpegFarmContext context) : base(context)
        {
        }

        public FfmpegTasks GetNext(TimeSpan timeout)
        {
            Context.Set<FfmpegJobs>().Load();

            DateTimeOffset latest = DateTimeOffset.UtcNow.Add(timeout);

            return Context.Set<FfmpegTasks>()
                .Where(t => t.TaskState == TranscodingJobState.Queued ||
                            (t.TaskState == TranscodingJobState.InProgress &&
                             t.Heartbeat < latest))
                .OrderByDescending(t => t.Job.Needed)
                .ThenBy(t => t.id)
                .FirstOrDefault();
        }
    }
}
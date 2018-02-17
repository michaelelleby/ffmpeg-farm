using System;
using System.Data.Entity;
using System.Linq;
using API.Database;
using Contract;

namespace API.Repository
{
    public class JobRepository : BaseRepository<FfmpegJobs>, IJobRepository
    {
        public JobRepository(FfmpegFarmContext context) : base(context)
        {
        }

        public bool PauseJob(Guid jobCorrelationId)
        {
            var job = Context.Set<FfmpegJobs>().SingleOrDefault(x => x.JobCorrelationId == jobCorrelationId);
            if (job == null)
                return false;

            foreach (var task in job.FfmpegTasks.Where(t => t.TaskState == TranscodingJobState.Queued))
            {
                task.TaskState = TranscodingJobState.Paused;
            }

            job.JobState = (byte)TranscodingJobState.Paused;

            return true;
        }

        public bool ResumeJob(Guid jobCorrelationId)
        {
            var job = Context.Set<FfmpegJobs>().SingleOrDefault(x => x.JobCorrelationId == jobCorrelationId);
            if (job == null)
                return false;

            foreach (var task in job.FfmpegTasks.Where(t => t.TaskState == TranscodingJobState.Paused))
            {
                task.TaskState = TranscodingJobState.Queued;
            }

            job.JobState = (byte)TranscodingJobState.Queued;

            return true;
        }

        public bool CancelJob(Guid jobCorrelationId)
        {
            var job = Context.Set<FfmpegJobs>().SingleOrDefault(x => x.JobCorrelationId == jobCorrelationId);
            if (job == null)
                return false;

            foreach (var task in job.FfmpegTasks.Where(t => t.TaskState == TranscodingJobState.Queued))
            {
                task.TaskState = TranscodingJobState.Canceled;
            }

            job.JobState = (byte)TranscodingJobState.Canceled;

            return true;
        }
    }
}
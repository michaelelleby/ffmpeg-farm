using System;
using System.Collections.Generic;
using System.Data;

namespace Contract
{
    public interface IVideoJobRepository : IJobRepository
    {
        TranscodingJob GetNextTranscodingJob();
        MergeJob GetNextMergeJob();
        Mp4boxJob GetNextDashJob();

        void SaveJobs(JobRequest job, ICollection<TranscodingJob> jobs, IDbConnection connection,
            Guid jobCorrelationId, int chunkDuration);
    }
}
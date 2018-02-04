using System;
using System.Collections.Generic;
using System.Data;

namespace Contract
{
    public interface IOldVideoJobRepository : IOldJobRepository
    {
        VideoTranscodingJob GetNextTranscodingJob();
        MergeJob GetNextMergeJob();
        Mp4boxJob GetNextDashJob();

        void SaveJobs(JobRequest job, ICollection<VideoTranscodingJob> jobs, IDbConnection connection,
            Guid jobCorrelationId, int chunkDuration);
    }
}
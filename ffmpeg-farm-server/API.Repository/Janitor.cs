using System;
using Contract;
using Dapper;

namespace API.Repository
{
    public class Janitor
    {
        protected readonly IHelper Helper;

        public Janitor(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            Helper = helper;
        }

        public void CleanUp()
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    conn.Execute(@"
DELETE
FROM FfmpegAudioRequest
WHERE [Created] < @filter

DELETE Targets
FROM FfmpegAudioRequestTargets Targets
INNER JOIN FFmpegjobs Jobs ON Targets.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE
FROM FfmpegHardSubtitlesRequest
WHERE [Created] < @filter

DELETE
FROM FfmpegLoudnessRequest
WHERE [Created] < @filter

DELETE
FROM FfmpegMuxRequest
WHERE [Created] < @filter

DELETE
FROM FfmpegScreenshotRequest
WHERE [Created] < @filter

DELETE
FROM FfmpegScrubbingRequest
WHERE [Created] < @filter

DELETE Tasks
FROM FfmpegTasks Tasks
INNER JOIN FFmpegjobs Jobs ON Tasks.FfmpegJobs_id = Jobs.id
WHERE Jobs.[Created] < @filter

DELETE VideoJobs
FROM FfmpegVideoJobs VideoJobs
INNER JOIN FFmpegjobs Jobs ON VideoJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE VideoMergeJobs
FROM FfmpegVideoMergeJobs VideoMergeJobs
INNER JOIN FFmpegjobs Jobs ON VideoMergeJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE VideoParts
FROM FfmpegVideoParts VideoParts
INNER JOIN FFmpegjobs Jobs ON VideoParts.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE
FROM FfmpegVideoRequest
WHERE [Created] < @filter

DELETE Targets
FROM FfmpegVideoRequestTargets Targets
INNER JOIN FFmpegjobs Jobs ON Targets.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE Mp4boxJobs
FROM Mp4boxJobs Mp4boxJobs
INNER JOIN FFmpegjobs Jobs ON Mp4boxJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE
FROM FFmpegjobs 
WHERE [Created] < @filter

DELETE
FROM Log 
WHERE [Logged] < @filter

", new {filter = DateTimeOffset.UtcNow.AddMonths(-2)});

                    scope.Complete();
                }
            }
        }
    }
}

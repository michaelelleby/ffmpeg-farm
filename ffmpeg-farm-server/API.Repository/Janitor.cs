using System;
using Contract;
using Dapper;

namespace API.Repository
{
    public class Janitor
    {
        protected readonly IHelper Helper;
        private readonly ILogging _logging;
        private const int BatchSize = 20000;

        public Janitor(IHelper helper, ILogging logging)
        {
            //if (helper == null)
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        public void CleanUp()
        {
            try
            {
                _logging.Debug("Janitor CleanUp started.");
                var total = 0;
                var loops = 0;
                int res;
                var filter = DateTimeOffset.UtcNow.AddMonths(-2);

                // part 1
                do
                {
                    using (var scope = TransactionUtils.CreateTransactionScope())
                    {
                        using (var conn = Helper.GetConnection())
                        {
                            res = conn.Execute(@"

DELETE TOP(@batchsize) Requests
FROM FfmpegAudioRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter
DELETE TOP(@batchsize) Targets
FROM FfmpegAudioRequestTargets Targets
INNER JOIN FFmpegjobs Jobs ON Targets.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Requests
FROM FfmpegHardSubtitlesRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Requests
FROM FfmpegLoudnessRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Requests
FROM FfmpegMuxRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Requests
FROM FfmpegScreenshotRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter


DELETE TOP(@batchsize) Requests
FROM FfmpegScrubbingRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Tasks
FROM FfmpegTasks Tasks
INNER JOIN FFmpegjobs Jobs ON Tasks.FfmpegJobs_id = Jobs.id
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) VideoJobs
FROM FfmpegVideoJobs VideoJobs
INNER JOIN FFmpegjobs Jobs ON VideoJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) VideoMergeJobs
FROM FfmpegVideoMergeJobs VideoMergeJobs
INNER JOIN FFmpegjobs Jobs ON VideoMergeJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) VideoParts
FROM FfmpegVideoParts VideoParts
INNER JOIN FFmpegjobs Jobs ON VideoParts.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Requests
FROM FfmpegVideoRequest Requests
INNER JOIN FFmpegjobs Jobs ON Requests.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Targets
FROM FfmpegVideoRequestTargets Targets
INNER JOIN FFmpegjobs Jobs ON Targets.JobCorrelationId = Jobs.JobCorrelationId 
WHERE Jobs.[Created] < @filter

DELETE TOP(@batchsize) Mp4boxJobs
FROM Mp4boxJobs Mp4boxJobs
INNER JOIN FFmpegjobs Jobs ON Mp4boxJobs.JobCorrelationId = Jobs.JobCorrelationId
WHERE Jobs.[Created] < @filter

", new {filter = filter, batchsize = BatchSize});

                            scope.Complete();
                        }
                    }

                    total += res;
                    loops++;
                } while (res > 0);

                // part 2
                do
                {
                    using (var scope = TransactionUtils.CreateTransactionScope())
                    {
                        using (var conn = Helper.GetConnection())
                        {
                            res = conn.Execute(@"
DELETE TOP(@batchsize)
FROM FFmpegjobs 
WHERE [Created] < @filter

DELETE TOP(@batchsize)
FROM Log 
WHERE [Logged] < @filter

", new { filter = filter, batchsize = BatchSize });

                            scope.Complete();
                        }
                    }

                    total += res;
                    loops++;
                } while (res > 0);


                _logging.Info($"Janitor CleanUp done. ({total} in {loops} batch(es))");
            }
            catch (Exception e)
            {
                _logging.Error(e, "Janitor failed.");
            }
        }
    }
}

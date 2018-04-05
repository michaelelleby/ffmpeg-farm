using System;
using System.Collections.Generic;
using Contract;
using Dapper;

namespace API.Repository
{
    public class ScreenshotJobRepository : JobRepository, IScreenshotJobRepository
    {
        private readonly IHelper _helper;

        public ScreenshotJobRepository(IHelper helper) : base(helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            _helper = helper;
        }

        public Guid Add(ScreenshotJobRequest request, ICollection<ScreenshotJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (jobs.Count == 0)
                throw new ArgumentException("Jobs parameter must contain at least 1 job", nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = _helper.GetConnection())
                {
                    connection.Execute(
                        "INSERT INTO FfmpegScreenshotRequest (JobCorrelationId, SourceFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @SourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            SourceFilename = request.VideoSourceFilename,
                            request.DestinationFilename,
                            request.OutputFolder,
                            request.Needed,
                            Created = DateTime.UtcNow
                        });

                    var jobId = connection.ExecuteScalar<int>(
                        "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @JobState, @JobType);SELECT @@IDENTITY;",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Created = DateTimeOffset.UtcNow,
                            request.Needed,
                            JobState = TranscodingJobState.Queued,
                            JobType = JobType.Screenshot
                        });

                    foreach (ScreenshotJob screenshotJob in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id, Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                screenshotJob.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                screenshotJob.DestinationFilename,
                                screenshotJob.DestinationDurationSeconds,
                                VerifyOutput = true
                            });
                    }
                }
                scope.Complete();
            }
            return jobCorrelationId;
        }
    }
}
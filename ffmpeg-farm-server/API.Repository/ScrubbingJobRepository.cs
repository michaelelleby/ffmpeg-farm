using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract;
using Dapper;

namespace API.Repository
{
    public class ScrubbingJobRepository : JobRepository, IScrubbingJobRepository
    {
        private readonly IHelper _helper;

        public ScrubbingJobRepository(IHelper helper) : base(helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            _helper = helper;
        }
        
        public Guid Add(ScrubbingJobRequest request, ICollection<ScrubbingJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (jobs.Count == 0) throw new ArgumentException("Jobs parameter must contain at least 1 job", nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = _helper.GetConnection())
                {
                    connection.Execute(
                        "INSERT INTO [FfmpegScrubbingRequest] (JobCorrelationId, SourceFilename, Needed, Created, OutputFolder, SpriteSheetSizes, ThumbnailResolutions, FirstThumbnailOffsetInSeconds, MaxSecondsBetweenThumbnails) VALUES(@JobCorrelationId, @SourceFilename, @Needed, @Created, @OutputFolder, @SpriteSheetSizes, @ThumbnailResolutions, @FirstThumbnailOffsetInSeconds, @MaxSecondsBetweenThumbnails);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            SourceFilename = request.SourceFilename,
                            Needed = request.Needed,
                            Created = DateTime.UtcNow,
                            OutputFolder = request.OutputFolder,
                            SpriteSheetSizes = string.Join(";", request.SpriteSheetSizes),
                            ThumbnailResolutions = string.Join(";", request.ThumbnailResolutions),
                            FirstThumbnailOffsetInSeconds = request.FirstThumbnailOffsetInSeconds,
                            MaxSecondsBetweenThumbnails = request.MaxSecondsBetweenThumbnails
                        });

                    var jobId = connection.ExecuteScalar<int>(
                        "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @JobState, @JobType);SELECT @@IDENTITY;",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Created = DateTimeOffset.UtcNow,
                            request.Needed,
                            JobState = TranscodingJobState.Queued,
                            JobType = JobType.Scrubbing
                        });

                    foreach (var scrubJob in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id,FfmpegExePath,  Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId,@FfmpegExePath, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                scrubJob.FfmpegExePath,
                                scrubJob.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                scrubJob.DestinationFilename,
                                scrubJob.DestinationDurationSeconds,
                                VerifyOutput = false
                            });
                    }
                }

                scope.Complete();

                return jobCorrelationId;
            }
        }
    }
}
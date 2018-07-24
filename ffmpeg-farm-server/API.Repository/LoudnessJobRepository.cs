using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract;
using Dapper;

namespace API.Repository
{
    public class LoudnessJobRepository : JobRepository, ILoudnessJobRepository
    {
        private readonly IHelper _helper;

        public LoudnessJobRepository(IHelper helper) : base(helper)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        public Guid Add(LoudnessJobRequest request, ICollection<LoudnessJob> jobs)
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
                        "INSERT INTO FfmpegLoudnessRequest (JobCorrelationId, SourceFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @SourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            SourceFilename = string.Join(" ", request.SourceFilenames),
                            request.DestinationFilename,
                            request.Needed,
                            request.OutputFolder,
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
                            JobType = JobType.Audio
                        });

                    foreach (LoudnessJob lj in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id,FfmpegExePath,  Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId,@FfmpegExePath, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                lj.FfmpegExePath,
                                lj.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                lj.DestinationFilename,
                                lj.DestinationDurationSeconds,
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
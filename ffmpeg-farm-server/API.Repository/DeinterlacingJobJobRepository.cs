using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Contract;
using Dapper;

namespace API.Repository
{
    /// <summary>
    /// Receives deinterlacing preprocess jobs orders.
    /// </summary>
    public class DeinterlacingJobJobRepository : JobRepository,IDeinterlacingJobRepository
    {
        private readonly string _connectionString;

        public DeinterlacingJobJobRepository(IHelper helper, string connectionString) : base(helper)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        public Guid Add(DeinterlacingJobRequest request, ICollection<FFmpegJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute(
                        "INSERT INTO FfmpegDeinterlacingRequest (JobCorrelationId, VideoSourceFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @VideoSourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            request.VideoSourceFilename,
                            request.Needed,
                            request.DestinationFilename,
                            request.OutputFolder,
                            Created = DateTime.UtcNow
                        });

                    foreach (DeinterlacingJob job in jobs.Select(x => x as DeinterlacingJob))
                    {
                        var jobId = connection.ExecuteScalar<int>(
                            "INSERT INTO FfmpegJobs (JobCorrelationId, Created, Needed, JobState, JobType) VALUES(@JobCorrelationId, @Created, @Needed, @State, @JobType);SELECT @@IDENTITY;",
                            new
                            {
                                JobCorrelationId = jobCorrelationId,
                                Created = DateTimeOffset.UtcNow,
                                job.Needed,
                                State = job.State,
                                JobType = JobType.Mux
                            });

                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id, FfmpegExePath, Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId,@FfmpegExePath, @Arguments, @QueuedState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                job.FfmpegExePath,
                                job.Arguments,
                                QueuedState = TranscodingJobState.Queued,
                                job.DestinationFilename,
                                job.DestinationDurationSeconds,
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
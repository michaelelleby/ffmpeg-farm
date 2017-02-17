using System;
using System.Collections.Generic;
using Contract;
using Dapper;

namespace API.Repository
{
    public class AudioJobRepository : JobRepository, IAudioJobRepository
    {
        private readonly IHelper _helper;

        public AudioJobRepository(IHelper helper) : base(helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _helper = helper;
        }

        public Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs)
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
                        "INSERT INTO FfmpegAudioRequest (JobCorrelationId, SourceFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @SourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            SourceFilename = string.Join(" ", request.SourceFilenames),
                            request.DestinationFilename,
                            request.Needed,
                            request.OutputFolder,
                            Created = DateTime.UtcNow
                        });

                    foreach (AudioDestinationFormat target in request.Targets)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegAudioRequestTargets (JobCorrelationId, Codec, Format, Bitrate) VALUES(@JobCorrelationId, @Codec, @Format, @Bitrate);",
                            new
                            {
                                jobCorrelationId,
                                Codec = target.AudioCodec.ToString(),
                                Format = target.Format.ToString(),
                                target.Bitrate
                            });
                    }

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

                    foreach (AudioTranscodingJob transcodingJob in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id, Arguments, TaskState, DestinationFilename, DestinationDurationSeconds, VerifyOutput) VALUES(@FfmpegJobsId, @Arguments, @TaskState, @DestinationFilename, @DestinationDurationSeconds, @VerifyOutput);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                transcodingJob.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                transcodingJob.DestinationFilename,
                                transcodingJob.DestinationDurationSeconds,
                                VerifyOutput = true
                            });
                    }
                }

                scope.Complete();

                return jobCorrelationId;
            }
        }
    }
}
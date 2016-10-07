using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Contract;
using Dapper;

namespace API.Repository
{
    public class AudioJobRepository : JobRepository, IAudioJobRepository
    {
        private readonly IHelper _helper;
        private readonly string _connectionString;

        public AudioJobRepository(IHelper helper, string connectionString) : base(helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException("connectionString");

            _helper = helper;
            _connectionString = connectionString;
        }

        public Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (jobs.Count == 0) throw new ArgumentException("Jobs parameter must contain at least 1 job", nameof(jobs));

            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = new TransactionScope())
            {
                using (var connection = _helper.GetConnection())
                {
                    connection.Execute(
                        "INSERT INTO FfmpegAudioRequest (JobCorrelationId, SourceFilename, DestinationFilename, OutputFolder, Needed, Created) VALUES(@JobCorrelationId, @SourceFilename, @DestinationFilename, @OutputFolder, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            request.SourceFilename,
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
                            "INSERT INTO FfmpegTasks (FfmpegJobs_id, Arguments, TaskState, DestinationFilename) VALUES(@FfmpegJobsId, @Arguments, @TaskState, @DestinationFilename);",
                            new
                            {
                                FfmpegJobsId = jobId,
                                transcodingJob.Arguments,
                                TaskState = TranscodingJobState.Queued,
                                transcodingJob.DestinationFilename
                            });
                    }
                }

                scope.Complete();

                return jobCorrelationId;
            }
        }

        public IEnumerable<AudioJobRequestDto> Get()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                // The dictionary is used for performance reasons, since we can add the specific jobs to each request
                // by using the job id as kesy
                IDictionary<Guid, AudioJobRequestDto> requests = new ConcurrentDictionary<Guid, AudioJobRequestDto>();

                var rows = connection.Query<AudioJobRequestDto>(
                    "SELECT JobCorrelationId, SourceFilename, DestinationFilename, Needed, Created, OutputFolder FROM FfmpegAudioRequest ORDER BY Id ASC;");
                foreach (AudioJobRequestDto requestDto in rows)
                {
                    requests.Add(requestDto.JobCorrelationId, requestDto);
                }

                var jobs = connection.Query<AudioTranscodingJobDto>(
                    "SELECT Arguments, JobCorrelationId, Needed, SourceFilename, State, Started, Heartbeat, HeartbeatMachineName, Progress FROM FfmpegTasks AJ " +
                    "INNER JOIN FfmpegJobs Jobs ON AJ.FfmpegJobs_id = Jobs.id " +
                    "ORDER BY Id ASC;");

                foreach (AudioTranscodingJobDto dto in jobs)
                {
                    requests[dto.JobCorrelationId].Jobs.Add(dto);
                }

                return requests.Values;
            }
        }

        public AudioJobRequestDto Get(Guid id)
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                var request = connection.QuerySingle<AudioJobRequestDto>(
                    "SELECT JobCorrelationId, SourceFilename, DestinationFilename, Needed, Created, OutputFolder FROM FfmpegAudioRequest WHERE JobCorrelationId = @JobCorrelationId;",
                    new {JobCorrelationId = id});

                request.Jobs = connection.Query<AudioTranscodingJobDto>(
                        "SELECT Arguments, JobCorrelationId, Needed, SourceFilename, State, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename, Bitrate FROM FfmpegTasks AJ " +
                        "INNER JOIN FfmpegJobs Jobs ON AJ.FfmpegJobs_id = Jobs.id " +
                        "WHERE JobCorrelationId = @JobCorrelationId;",
                        new {JobCorrelationId = id})
                    .ToList();

                return request;
            }
        }
    }
}
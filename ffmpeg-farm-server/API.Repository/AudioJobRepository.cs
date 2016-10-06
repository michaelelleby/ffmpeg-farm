using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Transactions;
using API.Service;
using Contract;
using Contract.Dto;
using Contract.Models;
using Dapper;

namespace API.Repository
{
    public class AudioJobRepository : JobRepository, IAudioJobRepository
    {
        private readonly string _connectionString;

        public AudioJobRepository(IHelper helper, string connectionString) : base(helper)
        {
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
                using (var connection = new SqlConnection(_connectionString))
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

                    foreach (AudioTranscodingJob transcodingJob in jobs)
                    {
                        connection.Execute(
                            "INSERT INTO FfmpegAudioJobs (JobCorrelationId, Arguments, Needed, SourceFilename, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @SourceFilename, @State);",
                            new
                            {
                                JobCorrelationId = jobCorrelationId,
                                transcodingJob.Arguments,
                                transcodingJob.Needed,
                                transcodingJob.SourceFilename,
                                transcodingJob.State
                            });
                    }
                }

                scope.Complete();

                return jobCorrelationId;
            }
        }

        public AudioTranscodingJob GetNextTranscodingJob()
        {
            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            DateTimeOffset timeout = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            var data = new
            {
                State = TranscodingJobState.InProgress,
                Heartbeat = DateTimeOffset.UtcNow,
                Timeout = timeout,
                QueuedState = TranscodingJobState.Queued,
                InProgressState = TranscodingJobState.InProgress
            };
            var parameters = new DynamicParameters(data);
            parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Arguments", dbType: DbType.AnsiStringFixedLength, direction: ParameterDirection.Output, size: 500);
            parameters.Add("@JobCorrelationId", dbType: DbType.Guid, direction: ParameterDirection.Output);

            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                do
                {
                    try
                    {
                        using (var transaction = connection.BeginTransaction())
                        {
                            var rowsUpdated = connection.Execute(
                                "UPDATE FfmpegAudioJobs SET State = @State, HeartBeat = @Heartbeat, Started = @Heartbeat, @Id = Id, @Arguments = Arguments, @JobCorrelationId = JobCorrelationId WHERE Id = (SELECT TOP 1 Id FROM FfmpegAudioJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Timeout) ORDER BY Needed ASC, Id ASC);",
                                parameters, transaction);
                            if (rowsUpdated == 0)
                                return null;

                            var job = new AudioTranscodingJob
                            {
                                Id = parameters.Get<int>("@Id"),
                                Arguments = parameters.Get<string>("@Arguments"),
                                JobCorrelationId = parameters.Get<Guid>("JobCorrelationId")
                            };

                            // Safety check to ensure that the data is being returned correctly in the SQL query
                            if (job.Id < 0 || string.IsNullOrWhiteSpace(job.Arguments) || job.JobCorrelationId == Guid.Empty)
                                throw new InvalidOperationException("One or more parameters were not set by SQL query.");

                            transaction.Commit();

                            return job;
                        }
                    }
                    catch (SqlException e)
                    {
                        // Retry in case of deadlocks
                        if (e.ErrorCode == 1205)
                        {
                            continue;
                        }

                        throw;
                    }
                } while (true);
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
                    "SELECT Arguments, JobCorrelationId, Needed, SourceFilename, State, Started, Heartbeat, HeartbeatMachineName, Progress FROM FfmpegAudioJobs ORDER BY Id ASC;");

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
                        "SELECT Arguments, JobCorrelationId, Needed, SourceFilename, State, Started, Heartbeat, HeartbeatMachineName, Progress FROM FfmpegAudioJobs WHERE JobCorrelationId = @JobCorrelationId;",
                        new {JobCorrelationId = id})
                    .ToList();

                return request;
            }
        }
    }
}
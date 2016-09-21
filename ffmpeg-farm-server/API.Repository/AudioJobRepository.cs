using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using Contract;
using Dapper;

namespace API.Repository
{
    public class AudioJobRepository : JobRepository, IAudioJobRepository
    {
        private readonly string _connectionString;

        public AudioJobRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        public Guid Add(AudioJobRequest request, ICollection<TranscodingJob> jobs)
        {
            Guid jobCorrelationId = Guid.NewGuid();

            using (var scope = new TransactionScope())
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute(
                        "INSERT INTO FfmpegAudioRequest (JobCorrelationId, SourceFilename, DestinationFilename, Needed, Created) VALUES(@JobCorrelationId, @SourceFilename, @DestinationFilename, @Needed, @Created);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            request.SourceFilename,
                            request.DestinationFilename,
                            request.Needed,
                            Created = DateTime.UtcNow,
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

                    foreach (TranscodingJob transcodingJob in jobs)
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
    }
}
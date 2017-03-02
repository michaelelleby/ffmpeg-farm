using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Contract;
using Contract.Dto;
using Dapper;

namespace API.Repository
{
    public class JobRepository : IJobRepository
    {
        protected readonly IHelper Helper;

        public JobRepository(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            Helper = helper;
        }

        public bool DeleteJob(Guid jobId)
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    int rowsDeleted = -1;

                    JobType jobType = GetJobType(jobId, connection);

                    switch (jobType)
                    {
                        case JobType.Audio:
                        case JobType.Mux:
                            rowsDeleted = DeleteMuxJob(jobId, connection);
                            break;
                        case JobType.Video:
                            rowsDeleted = DeleteVideoJob(jobId, connection);
                            break;
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                            return false;
                        default:
                            throw new InvalidOperationException();
                    }

                    scope.Complete();

                    return rowsDeleted > 0;
                }
            }
        }

        private static JobType GetJobType(Guid jobId, IDbConnection connection)
        {
            return connection.QuerySingleOrDefault<JobType>("SELECT TOP 1 JobType FROM FfmpegJobs WHERE JobCorrelationId = @Id;",
                new {Id = jobId});
        }

        private static int DeleteMuxJob(Guid jobId, IDbConnection connection)
        {
            const string sql = "DELETE Tasks FROM FfmpegTasks Tasks INNER JOIN FFmpegjobs Jobs ON Tasks.FfmpegJobs_id = Jobs.id WHERE Jobs.JobCorrelationId = @Id;" +
                               "DELETE FROM FfmpegMuxRequest WHERE JobCorrelationId = @Id;" +
                               "DELETE FROM FfmpegJobs WHERE JobCorrelationId = @Id;";

            return connection.Execute(sql, new { Id = jobId });
        }

        public bool PauseJob(Guid jobId)
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    JobType jobType = GetJobType(jobId, conn);

                    string sql = "UPDATE FfmpegJobs SET JobState = @PausedState WHERE JobCorrelationId = @Id AND JobState = @QueuedState;";
                    switch (jobType)
                    {
                        case JobType.Audio:
                        case JobType.Mux:
                            sql += "UPDATE Tasks SET Tasks.TaskState = @PausedState FROM FfmpegTasks Tasks INNER JOIN FfmpegJobs Jobs ON Jobs.id = Tasks.FfmpegJobs_Id WHERE Jobs.JobCorrelationId = @Id AND Tasks.TaskState = @QueuedState;";
                            break;
                        case JobType.Video:
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    int updatedRows = conn.Execute(sql,
                        new {Id = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued});

                    scope.Complete();

                    return updatedRows > 0;
                }
            }
        }

        public bool ResumeJob(Guid jobId)
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    JobType jobType = GetJobType(jobId, conn);

                    string sql = "UPDATE FfmpegJobs SET JobState = @QueuedState WHERE JobCorrelationId = @Id AND State = @PausedState;";

                    switch (jobType)
                    {
                        case JobType.Audio:
                        case JobType.Mux:
                            sql += "UPDATE Tasks SET Tasks.TaskState = @QueuedState FROM FfmpegTasks Tasks INNER JOIN FfmpegJobs Jobs ON Jobs.id = Tasks.FfmpegJobs_Id WHERE Jobs.JobCorrelationId = @Id AND Tasks.TaskState = @PausedState;";
                            break;
                        case JobType.Video:
                        case JobType.VideoMp4box:
                        case JobType.VideoMerge:
                            throw new NotImplementedException();
                        case JobType.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    int updatedRows = conn.Execute(sql,
                        new {Id = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued});

                    scope.Complete();

                    return updatedRows > 0;
                }
            }
        }

        public bool CancelJob(Guid jobId)
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var conn = Helper.GetConnection())
                {
                    string sql = "UPDATE FfmpegJobs SET JobState = @CanceledState WHERE JobCorrelationId = @Id AND jobState <> @DoneState AND jobState <> @FailedState;";
                    sql += "UPDATE Tasks SET Tasks.TaskState = @CanceledState FROM FfmpegTasks Tasks INNER JOIN FfmpegJobs Jobs ON Jobs.id = Tasks.FfmpegJobs_Id " +  
                           "WHERE Jobs.JobCorrelationId = @Id AND Tasks.TaskState <> @DoneState AND Tasks.TaskState <> @FailedState;";
                    
                    int updatedRows = conn.Execute(sql,
                        new { Id = jobId, CanceledState = TranscodingJobState.Canceled, DoneState = TranscodingJobState.Done, FailedState = TranscodingJobState.Failed });

                    scope.Complete();

                    return updatedRows > 0;
                }
            }
        }

        public IEnumerable<JobRequestDto> GetJobStatuses(Guid jobCorrelationId = default(Guid))
        {
            IEnumerable<TranscodingJobDto> jobs;
            IEnumerable<JobRequestDto> requests;
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                if (jobCorrelationId != default(Guid))
                {
                    requests =
                        connection.Query<JobRequestDto>("SELECT * from FfmpegAudioRequest WHERE JobCorrelationId = @JobCorrelationId;",
                                new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                    jobs =
                        connection.Query<TranscodingJobDto>(
                                "SELECT * FROM FfmpegTasks WHERE JobCorrelationId = @JobCorrelationId;",
                                new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                }
                else
                {
                    requests = connection.Query<JobRequestDto>("SELECT * from FfmpegAudioRequest").ToList();
                    jobs = connection.Query<TranscodingJobDto>("SELECT * FROM FfmpegTasks").ToList();
                }
            }

            return requests;
        }

        public TranscodingJobState SaveProgress(int jobId, bool failed, bool done, TimeSpan progress, string machineName)
        {
            InsertClientHeartbeat(machineName);

            TranscodingJobState jobState = failed ? TranscodingJobState.Failed : done ? TranscodingJobState.Done
                                                    : TranscodingJobState.InProgress;

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    connection.Open();

                    // Only allow progress updates for tasks which have statetaskState = InProgress
                    // This will prevent out-of-order updates causing tasks set to either Failed or Done
                    // to be set back to InProgress
                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegTasks SET Progress = @Progress, Heartbeat = @Heartbeat, TaskState = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id" +
                        " AND TaskState = @InProgressState;",
                        new
                        {
                            Id = jobId,
                            Progress = progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            InProgressState = TranscodingJobState.InProgress,
                            machineName
                        });

                    jobState = (TranscodingJobState) connection.QuerySingle<int>("SELECT TaskState FROM FfmpegTasks WHERE id = @Id;",
                        new
                        {
                            Id = jobId
                        });

                    if (updatedRows != 1 && jobState != TranscodingJobState.Canceled)
                        throw new Exception($"Failed to update progress for job id {jobId}");

                    scope.Complete();
                }
            }
            return jobState;
        }

        public FFmpegTaskDto GetNextJob(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName)) throw new ArgumentNullException(nameof(machineName));

            InsertClientHeartbeat(machineName);

            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            var now = DateTimeOffset.UtcNow;

            DateTimeOffset timeout = now.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            do
            {
                using (var scope = TransactionUtils.CreateTransactionScope())
                {
                    using (var connection = Helper.GetConnection())
                    {
                        connection.Open();

                        try
                        {
                            var data = new
                            {
                                QueuedState = TranscodingJobState.Queued,
                                InProgressState = TranscodingJobState.InProgress,
                                Timeout = timeout,
                                Timestamp = now
                            };

                            var task = connection.QuerySingleOrDefault<FFmpegTaskDto>("sp_GetNextTask", data, commandType: CommandType.StoredProcedure);
                            if (task == null)
                                return null;

                            // Safety check to ensure that the data is being returned correctly in the SQL query
                            if (task.Id < 0 || task.FfmpegJobsId < 0 || string.IsNullOrWhiteSpace(task.Arguments))
                                throw new InvalidOperationException("One or more parameters were not set by SQL query.");

                            scope.Complete();

                            return task;
                        }
                        catch (SqlException e)
                        {
                            // Retry in case of deadlocks
                            if (e.Number == 1205)
                            {
                                continue;
                            }

                            throw;
                        }
                    }
                }
            } while (true);
        }

        public ICollection<FFmpegJobDto> Get(int take = 10)
        {
            IDictionary<int, ICollection<FFmpegTaskDto>> jobsDictionary = new ConcurrentDictionary<int, ICollection<FFmpegTaskDto>>();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    ICollection<FFmpegJobDto> jobs = connection.Query<FFmpegJobDto>(
                        "SELECT top(" + take + @") id, JobCorrelationId, Created, Needed, JobType, JobState AS State
                        FROM FfmpegJobs
                        ORDER BY Created DESC"
                    )
                        .ToList();

                    if (jobs == null || jobs.Count == 0)
                        return new List<FFmpegJobDto>();

                    List<string> ids = jobs.Select(j => j.Id.ToString()).ToList();
                    string jobidSql = "(" + ids.Aggregate((a, b) => a + "," + b) + ")";

                    ICollection<FFmpegTaskDto> tasks = connection.Query<FFmpegTaskDto>(
                            @"SELECT id, FfmpegJobs_id AS FfmpegJobsId, Arguments, TaskState AS State, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationDurationSeconds, DestinationFilename 
                              FROM FfmpegTasks
                              WHERE FfmpegJobs_id in " + jobidSql
                        )
                        .ToList();

                    foreach (FFmpegTaskDto task in tasks)
                    {
                        if (!jobsDictionary.ContainsKey(task.FfmpegJobsId))
                        {
                            jobsDictionary.Add(task.FfmpegJobsId, new List<FFmpegTaskDto>());
                        }

                        jobsDictionary[task.FfmpegJobsId].Add(task);
                    }

                    foreach (FFmpegJobDto job in jobs.Where(x => jobsDictionary.ContainsKey(x.Id)))
                    {
                        job.Tasks = jobsDictionary[job.Id];
                    }

                    scope.Complete();

                    return jobs;
                }
            }
        }

        public FFmpegJobDto Get(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentOutOfRangeException("id");

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    var job = connection.QuerySingleOrDefault<FFmpegJobDto>(
                        "SELECT id, JobCorrelationId, Needed, Created, JobType, JobState AS State FROM FfmpegJobs WHERE JobCorrelationId = @Id;",
                        new {id});
                    if (job == null)
                        return null;

                    job.Tasks = connection.Query<FFmpegTaskDto>(
                        "SELECT id, FfmpegJobs_id, Arguments, TaskState AS State, DestinationDurationSeconds, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename FROM FfmpegTasks WHERE FfmpegJobs_id = @Id;",
                        new {job.Id})
                        .ToList();

                    scope.Complete();

                    return job;
                }
            }
        }

        public Guid GetGuidById(int id)
        {
            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    return connection.QueryFirstOrDefault<Guid>("SELECT JobCorrelationId FROM FfmpegJobs WHERE id = @Id;", new {id});
                }
            }
        }

        private void InsertClientHeartbeat(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName)) throw new ArgumentNullException(nameof(machineName));

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                using (var connection = Helper.GetConnection())
                {
                    int rowsAffected = connection.Execute("sp_InsertClientHeartbeat", new
                    {
                        MachineName = machineName,
                        Timestamp = DateTimeOffset.UtcNow
                    }, commandType: CommandType.StoredProcedure);

                    if (rowsAffected != 1)
                        throw new Exception($"sp_InsertClientHeartbeat affected {rowsAffected} rows, should only affect 1 row!");

                    scope.Complete();
                }
            }
        }

        private string GetJobArguments(JobType jobType, int id, IDbConnection connection)
        {
            switch (jobType)
            {
                case JobType.Audio:
                    return connection.ExecuteScalar<string>("SELECT Arguments FROM FfmpegTasks WHERE FfmpegJobs_id = @Id;",
                        new {id});
                case JobType.Video:
                case JobType.VideoMp4box:
                case JobType.VideoMerge:
                    throw new NotImplementedException();
                case JobType.Mux:
                    return connection.ExecuteScalar<string>("SELECT Arguments FROM FfmpegTasks WHERE FfmpegJobs_Id = @Id;",
                        new {id});
                case JobType.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, null);
            }
        }

        private static bool PauseVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @PausedState WHERE JobCorrelationId = @JobId AND State = @QueuedState",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            return rowsUpdated > 0;
        }

        private static bool ResumeVideoJob(Guid jobId, IDbConnection conn)
        {
            var rowsUpdated = conn.Execute("UPDATE FfmpegVideoJobs SET State = @QueuedState WHERE JobCorrelationId = @JobId AND State = @PausedState;",
                new { JobId = jobId, PausedState = TranscodingJobState.Paused, QueuedState = TranscodingJobState.Queued });

            return rowsUpdated > 0;
        }

        private static int DeleteVideoJob(Guid jobId, IDbConnection connection)
        {
            connection.Execute("DELETE FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegVideoParts WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

            connection.Execute("DELETE FROM FfmpegVideoRequestTargets WHERE JobCorrelationId = @Id;",
                new {Id = jobId});

            int rowsDeleted = connection.Execute("DELETE FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id;",
                new {Id = jobId});
            return rowsDeleted;
        }

        private static void UpdateVideoJob(BaseJob job, IDbConnection connection)
        {
            JobRequest jobRequest = connection.Query<JobRequest>(
                    "SELECT JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, EnableDash, EnablePsnr FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id",
                    new {Id = job.JobCorrelationId})
                .SingleOrDefault();
            if (jobRequest == null)
                throw new ArgumentException($@"Job with correlation id {job.JobCorrelationId} not found");

            jobRequest.Targets = connection.Query<DestinationFormat>(
                    "SELECT JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate FROM FfmpegVideoRequestTargets WHERE JobCorrelationId = @Id;",
                    new {Id = job.JobCorrelationId})
                .ToArray();

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                Type jobType = job.GetType();
                TranscodingJobState jobState = job.Failed
                    ? TranscodingJobState.Failed
                    : job.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                if (jobType == typeof(VideoTranscodingJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegVideoJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
                        new
                        {
                            Id = job.Id,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName,
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");

                    if (jobRequest.EnablePsnr)
                    {
                        foreach (FfmpegPart chunk in ((VideoTranscodingJob) job).Chunks)
                        {
                            connection.Execute(
                                "UPDATE FfmpegVideoParts SET PSNR = @Psnr WHERE Id = @Id;",
                                new {Id = chunk.Id, Psnr = chunk.Psnr});
                        }
                    }
                }
                else if (jobType == typeof(MergeJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegMergeJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
                        new
                        {
                            Id = job.Id,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");

                    var states = connection.Query<string>(
                            "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;",
                            new {Id = job.JobCorrelationId})
                        .Select(value => Enum.Parse(typeof(TranscodingJobState), value))
                        .Cast<TranscodingJobState>();

                    if (states.All(x => x == TranscodingJobState.Done))
                    {
                        string tempFolder = String.Concat(Path.GetDirectoryName(jobRequest.DestinationFilename),
                            Path.DirectorySeparatorChar, jobRequest.JobCorrelationId.ToString("N"));

                        Directory.Delete(tempFolder, true);
                    }
                }
                else if (jobType == typeof(Mp4boxJob))
                {
                    int updatedRows = connection.Execute(
                        "UPDATE Mp4boxJobs SET Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE JobCorrelationId = @Id;",
                        new
                        {
                            Id = job.JobCorrelationId,
                            Progress = job.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            State = jobState,
                            MachineName = job.MachineName
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {job.Id}");
                }

                scope.Complete();
            }

            using (var scope = TransactionUtils.CreateTransactionScope())
            {
                ICollection<TranscodingJobState> totalJobs = connection.Query<TranscodingJobState>(
                        "SELECT State FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();

                if (totalJobs.Any(x => x != TranscodingJobState.Done))
                {
                    // Not all transcoding jobs are finished
                    return;
                }

                totalJobs = connection.Query<TranscodingJobState>(
                        "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();
                if (totalJobs.Any(x => x != TranscodingJobState.Done))
                {
                    // Not all merge jobs are finished
                    return;
                }

                string destinationFilename = jobRequest.DestinationFilename;
                string fileNameWithoutExtension =
                    Path.GetFileNameWithoutExtension(destinationFilename);
                string fileExtension = Path.GetExtension(destinationFilename);
                string outputFolder = Path.GetDirectoryName(destinationFilename);

                if (totalJobs.Count == 0)
                {
                    QueueMergeJob(job, connection, outputFolder, fileNameWithoutExtension, fileExtension, jobRequest);
                }
                else if (jobRequest.EnableDash)
                {
                    QueueMpegDashMergeJob(job, destinationFilename, connection, jobRequest, fileNameWithoutExtension,
                        outputFolder, fileExtension);
                }

                scope.Complete();
            }
        }

        private static void QueueMpegDashMergeJob(BaseJob job, string destinationFilename, IDbConnection connection,
            JobRequest jobRequest, string fileNameWithoutExtension, string outputFolder, string fileExtension)
        {
            string destinationFolder = Path.GetDirectoryName(destinationFilename);
            ICollection<TranscodingJobState> totalJobs =
                connection.Query<TranscodingJobState>("SELECT State FROM Mp4boxJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .ToList();

            // One MPEG DASH merge job is already queued. Do nothing
            if (totalJobs.Any())
                return;

            string arguments =
                $@"-dash 4000 -rap -frag-rap -profile onDemand -out {destinationFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}.mpd";

            var chunks = connection.Query<FfmpegPart>(
                "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegVideoParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                new {Id = job.JobCorrelationId});
            foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
            {
                int targetNumber = chunk.First().Target;
                DestinationFormat target = jobRequest.Targets[targetNumber];

                string targetFilename =
                    $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{target.Width}x{target
                        .Height}_{target.VideoBitrate}_{target.AudioBitrate}{fileExtension}";

                arguments += $@" {targetFilename}";
            }

            connection.Execute(
                "INSERT INTO Mp4boxJobs (JobCorrelationId, Arguments, Needed, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @State);",
                new
                {
                    JobCorrelationId = jobRequest.JobCorrelationId,
                    Arguments = arguments,
                    Needed = jobRequest.Needed,
                    State = TranscodingJobState.Queued
                });
        }

        private static void QueueMergeJob(BaseJob job, IDbConnection connection, string outputFolder,
            string fileNameWithoutExtension, string fileExtension, JobRequest jobRequest)
        {
            ICollection<FfmpegPart> chunks = connection.Query<FfmpegPart>(
                    "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegVideoRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename, Width, Height, Bitrate FROM FfmpegVideoParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                    new {Id = job.JobCorrelationId})
                .ToList();

            foreach (IEnumerable<FfmpegPart> chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
            {
                var FfmpegVideoParts = chunk as IList<FfmpegPart> ?? chunk.ToList();
                int targetNumber = FfmpegVideoParts.First().Target;
                DestinationFormat target = jobRequest.Targets[targetNumber];

                string targetFilename =
                    $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{target.Width}x{target.Height}_{target.VideoBitrate}_{target.AudioBitrate}{fileExtension}";

                // TODO Implement proper detection if files are already merged
                if (File.Exists(targetFilename))
                    continue;

                string path =
                    $"{outputFolder}{Path.DirectorySeparatorChar}{job.JobCorrelationId.ToString("N")}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{targetNumber}.list";

                using (TextWriter tw = new StreamWriter(path))
                {
                    foreach (FfmpegPart part in FfmpegVideoParts.Where(x => x.IsAudio == false))
                    {
                        tw.WriteLine($"file '{part.Filename}'");
                    }
                }
                string audioSource = chunks.Single(x => x.IsAudio && x.Bitrate == target.AudioBitrate).Filename;

                string arguments =
                    $@"-y -f concat -safe 0 -i ""{path}"" -i ""{audioSource}"" -c copy {targetFilename}";

                connection.Execute(
                    "INSERT INTO FfmpegMergeJobs (JobCorrelationId, Arguments, Needed, State, Target) VALUES(@JobCorrelationId, @Arguments, @Needed, @State, @Target);",
                    new
                    {
                        JobCorrelationId = job.JobCorrelationId,
                        Arguments = arguments,
                        Needed = jobRequest.Needed,
                        State = TranscodingJobState.Queued,
                        Target = targetNumber
                    });
            }
        }
    }
}
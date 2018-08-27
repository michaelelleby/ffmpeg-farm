using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Contract;
using Contract.Dto;
using Dapper;
using IsolationLevel = System.Transactions.IsolationLevel;

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

        public TranscodingJobState SaveProgress(int id, bool failed, bool done, TimeSpan progress, TimeSpan? verifyProgress, string machineName)
        {
            InsertClientHeartbeat(machineName);

            TranscodingJobState jobState = failed
                ? TranscodingJobState.Failed
                : done
                    ? TranscodingJobState.Done
                    : TranscodingJobState.InProgress;

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                // Only allow progress updates for tasks which have statetaskState = InProgress
                // This will prevent out-of-order updates causing tasks set to either Failed or Done
                // to be set back to InProgress
                int updatedRows = connection.Execute(
                    "UPDATE FfmpegTasks SET Progress = @Progress, VerifyProgress = @VerifyProgress, Heartbeat = @Heartbeat, TaskState = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id" +
                    " AND TaskState = @InProgressState;",
                    new
                    {
                        Id = id,
                        Progress = progress.TotalSeconds,
                        VerifyProgress = verifyProgress?.TotalSeconds,
                        Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                        State = jobState,
                        InProgressState = TranscodingJobState.InProgress,
                        machineName
                    });

                if (updatedRows != 1)
                    throw new Exception($"Failed to update progress for task id {id}");
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

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                IEnumerable<FFmpegJobDto> existingJobs = GetActiveJobs(machineName, connection);
                if (existingJobs.Any(x => (x.Type == JobType.HardSubtitles || x.Type == JobType.StereoTool)
                                           && (x.Tasks?.All(t=> !t.Heartbeat.HasValue || t.Heartbeat.Value > timeout) ?? true)))
                {
                    // If client is already working on either HardSubs or StereoTool, then don't assign new jobs to that client yet (they should finish their homework first!).
                    return null;
                }
            }

            do
            {
                using (var scope = TransactionUtils.CreateTransactionScope(IsolationLevel.Serializable))
                {
                    using (var connection = Helper.GetConnection())
                    {
                        try
                        {
                            var data = new
                            {
                                QueuedState = TranscodingJobState.Queued,
                                InProgressState = TranscodingJobState.InProgress,
                                Timeout = timeout,
                                Timestamp = now
                            };

                            connection.Open();

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

        private static IEnumerable<FFmpegJobDto> GetActiveJobs(string machineName, IDbConnection connection)
        {
            return connection.Query<FFmpegJobDto>(
                @"SELECT FfmpegJobs.id, JobCorrelationId, Created, Needed, JobType AS Type, JobState AS State FROM FfmpegJobs WITH (NOLOCK)
	INNER JOIN FfmpegTasks WITH (NOLOCK) ON FfmpegJobs.id = FfmpegTasks.FfmpegJobs_id
	WHERE FfmpegTasks.HeartbeatMachineName = @MachineName AND FfmpegTasks.TaskState = @State;"
                , new {machineName, State = TranscodingJobState.InProgress});
        }

        public ICollection<FFmpegJobDto> Get(int take = 10)
        {
            IDictionary<int, ICollection<FFmpegTaskDto>> jobsDictionary = new ConcurrentDictionary<int, ICollection<FFmpegTaskDto>>();
            ICollection<FFmpegJobDto> jobs = null;
            ICollection<FFmpegTaskDto> tasks = null;
            using (var connection = Helper.GetConnection())
            {
                jobs = connection.Query<FFmpegJobDto>(
                        "SELECT top(" + take + @") id, JobCorrelationId, Created, Needed, JobType, JobState AS State
                        FROM FfmpegJobs
                        ORDER BY Created DESC"
                    )
                    .ToList();

                if (jobs == null || jobs.Count == 0)
                    return new List<FFmpegJobDto>();

                List<string> ids = jobs.Select(j => j.Id.ToString()).ToList();
                string jobidSql = "(" + ids.Aggregate((a, b) => a + "," + b) + ")";

                tasks = connection.Query<FFmpegTaskDto>(
                        @"SELECT id, FfmpegJobs_id AS FfmpegJobsId, FfmpegExePath, Arguments, TaskState AS State, Started, Heartbeat, HeartbeatMachineName, Progress, VerifyProgress, DestinationDurationSeconds, DestinationFilename 
                              FROM FfmpegTasks
                              WHERE FfmpegJobs_id in " + jobidSql
                    )
                    .ToList();
            }

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

            return jobs;
        }

        public FFmpegJobDto Get(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentOutOfRangeException("id");

            using (var connection = Helper.GetConnection())
            {
                var job = connection.QuerySingleOrDefault<FFmpegJobDto>(
                    "SELECT id, JobCorrelationId, Needed, Created, JobType, JobState AS State FROM FfmpegJobs WHERE JobCorrelationId = @Id;",
                    new {id});
                if (job == null)
                    return null;

                job.Tasks = connection.Query<FFmpegTaskDto>(
                        "SELECT id, FfmpegJobs_id, FfmpegExePath, Arguments, TaskState AS State, DestinationDurationSeconds, Started, Heartbeat, HeartbeatMachineName, Progress, VerifyProgress, DestinationFilename FROM FfmpegTasks WHERE FfmpegJobs_id = @Id;",
                        new {job.Id})
                    .ToList();

                return job;
            }
        }

        public Guid GetGuidById(int id)
        {
            using (var connection = Helper.GetConnection())
            {
                return connection.QueryFirstOrDefault<Guid>("SELECT JobCorrelationId FROM FfmpegJobs WHERE id = @Id;", new {id});
            }
        }

        private void InsertClientHeartbeat(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName)) throw new ArgumentNullException(nameof(machineName));

            using (var connection = Helper.GetConnection())
            {
                int rowsAffected = connection.Execute("sp_InsertClientHeartbeat", new
                {
                    MachineName = machineName,
                    Timestamp = DateTimeOffset.UtcNow
                }, commandType: CommandType.StoredProcedure);

                if (rowsAffected != 1)
                {
                    throw new Exception($"sp_InsertClientHeartbeat affected {rowsAffected} rows, should only affect 1 row!");
                }
            }
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
        
        public int PruneInactiveClients(TimeSpan maxAge)
        {
            using (var scope = TransactionUtils.CreateTransactionScope(IsolationLevel.Serializable))
            {
                using (var connection = Helper.GetConnection())
                {
                    var res = connection.Execute("DELETE FROM Clients WHERE Clients.LastHeartbeat < @MaxAge",
                        new {MaxAge = DateTimeOffset.Now - maxAge });
                    scope.Complete();
                    return res;
                }
            }
        }
    }
}
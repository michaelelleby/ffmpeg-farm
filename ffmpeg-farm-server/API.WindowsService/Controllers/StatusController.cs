using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        public JobResult GetStatus()
        {
            IEnumerable<dynamic> jobs;
            IEnumerable<JobResultModel> requests;
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                requests = connection.Query<JobResultModel>("SELECT * from FfmpegRequest").ToList();
                jobs = connection.Query<TranscodingJob>("SELECT * FROM FfmpegJobs").ToList();
            }

            foreach (dynamic request in requests)
            {
                request.Jobs = jobs.Where(x => x.JobCorrelationId == request.JobCorrelationId);
            }

            return new JobResult
            {
                Requests = requests
            };
        }

        public void PutProgressUpdate(BaseJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.MachineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            Helper.InsertClientHeartbeat(job.MachineName);

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                var jobRequest = connection.Query<dynamic>(
                    "SELECT JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, EnableDash FROM FfmpegRequest WHERE JobCorrelationId = ?",
                    new {job.JobCorrelationId})
                    .SingleOrDefault();
                if (jobRequest == null)
                    throw new ArgumentException($@"Job with correlation id {job.JobCorrelationId} not found");

                using (var transaction = connection.BeginTransaction())
                {
                    Type jobType = job.GetType();
                    TranscodingJobState jobState = job.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                    if (jobType == typeof(TranscodingJob))
                    {
                        int updatedRows = connection.Execute(
                            "UPDATE FfmpegJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State WHERE Id = @Id;",
                            new
                            {
                                Id = job.Id,
                                Progress = job.Progress.TotalSeconds,
                                Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                                State = jobState
                            });

                        if (updatedRows != 1)
                            throw new Exception($"Failed to update progress for job id {job.Id}");
                    }
                    else if (jobType == typeof(MergeJob))
                    {
                        int updatedRows = connection.Execute(
                            "UPDATE FfmpegMergeJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State WHERE Id = @Id;",
                            new
                            {
                                Id = job.Id,
                                Progress = job.Progress.TotalSeconds,
                                Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                                State = jobState
                            });

                        if (updatedRows != 1)
                            throw new Exception($"Failed to update progress for job id {job.Id}");
                    }
                    else if (jobType == typeof(Mp4boxJob))
                    {
                        int updatedRows = connection.Execute(
                            "UPDATE Mp4boxJobs SET Heartbeat = @Heartbeat, State = @State WHERE JobCorrelationId = @Id;",
                            new
                            {
                                Id = job.JobCorrelationId,
                                Progress = job.Progress.TotalSeconds,
                                Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                                State = jobState
                            });

                        if (updatedRows != 1)
                            throw new Exception($"Failed to update progress for job id {job.Id}");
                    }
                    transaction.Commit();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    ICollection<TranscodingJobState> totalJobs = connection.Query(
                            "SELECT State FROM FfmpegJobs WHERE JobCorrelationId = ?;",
                            new { jobRequest.JobCorrelationId })
                            .Select(x => (TranscodingJobState)Enum.Parse(typeof(TranscodingJobState), x.State))
                            .ToList();

                    if (totalJobs.Any(x => x != TranscodingJobState.Done))
                    {
                        // Not all transcoding jobs are finished
                        return;
                    }

                    totalJobs = connection.Query(
                        "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = ?;",
                        new {jobRequest.JobCorrelationId})
                        .Select(x => (TranscodingJobState) Enum.Parse(typeof(TranscodingJobState), x.State))
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
                        var chunks = connection.Query<FfmpegPart>(
                            "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                            new {Id = job.JobCorrelationId});

                        foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
                        {
                            int targetNumber = chunk.First().Target;

                            string targetFilename =
                                $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{targetNumber}{fileExtension}";

                            // TODO Implement proper detection if files are already merged
                            if (File.Exists(targetFilename))
                                continue;

                            string path = string.Format("{0}{1}{2}_{3}.list",
                                outputFolder,
                                Path.DirectorySeparatorChar,
                                fileNameWithoutExtension,
                                targetNumber);

                            using (TextWriter tw = new StreamWriter(path))
                            {
                                foreach (FfmpegPart part in chunk.Where(x => x.IsAudio == false))
                                {
                                    tw.WriteLine($"file '{part.Filename}'");
                                }
                            }
                            string audioSource = chunk.Single(x => x.IsAudio).Filename;

                            string arguments =
                                $@"-y -f concat -safe 0 -i ""{path}"" -i ""{audioSource}"" -c copy {targetFilename}";

                            int duration = Helper.GetDuration(jobRequest.VideoSourceFilename);
                            connection.Execute(
                                "INSERT INTO FfmpegMergeJobs (JobCorrelationId, Arguments, Needed, State) VALUES(?, ?, ?, ?);",
                                new
                                {
                                    job.JobCorrelationId,
                                    arguments,
                                    jobRequest.Needed,
                                    TranscodingJobState.Queued
                                });
                        }
                    }
                    else if (jobRequest.EnableDash)
                    {
                        string destinationFolder = Path.GetDirectoryName(destinationFilename);
                        totalJobs = connection.Query("SELECT State FROM Mp4boxJobs WHERE JobCorrelationId = ?;",
                            new { jobRequest.JobCorrelationId })
                            .Select(x => (TranscodingJobState)Enum.Parse(typeof(TranscodingJobState), x.State))
                            .ToList();

                        if (totalJobs.Any() == false)
                        {
                            string arguments =
                                $@"-dash 4000 -rap -frag-rap -profile onDemand -out {destinationFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}.mpd";

                            var chunks = connection.Query<FfmpegPart>(
                                "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                                new {Id = job.JobCorrelationId});
                            foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
                            {
                                int targetNumber = chunk.First().Target;

                                string targetFilename =
                                    $@"{outputFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}_{targetNumber}{fileExtension}";

                                arguments += $@" {targetFilename}";
                            }

                            connection.Execute(
                                "INSERT INTO Mp4boxJobs (JobCorrelationId, Arguments, Needed, State) VALUES(?, ?, ?, ?);",
                                new {jobRequest.JobCorrelationId, arguments, jobRequest.Needed, TranscodingJobState.Queued});
                        }
                    }

                    transaction.Commit();
                }
            }
        }
    }
}

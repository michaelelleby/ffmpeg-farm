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
                requests = connection.Query<JobResultModel>("SELECT * from FfmpegRequest").ToList();
                jobs = connection.Query("SELECT * FROM FfmpegJobs").ToList();
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

        public void PutProgressUpdate(TranscodingJob transcodingJob)
        {
            if (transcodingJob == null) throw new ArgumentNullException(nameof(transcodingJob));
            if (string.IsNullOrWhiteSpace(transcodingJob.MachineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                Helper.InsertClientHeartbeat(transcodingJob.MachineName, connection);

                using (var transaction = connection.BeginTransaction())
                {
                    connection.Execute("INSERT OR REPLACE INTO Clients (MachineName, LastHeartbeat) VALUES(?, ?);",
                        new {transcodingJob.MachineName, DateTimeOffset.UtcNow});

                    var jobRequest = connection.Query<dynamic>(
                        "SELECT JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed FROM FfmpegRequest WHERE JobCorrelationId = ?",
                        new {transcodingJob.JobCorrelationId})
                        .SingleOrDefault();
                    if (jobRequest == null)
                        throw new ArgumentException(
                            $@"Job with correlation id {transcodingJob.JobCorrelationId} not found");

                    int updatedRows = connection.Execute(
                        "UPDATE FfmpegJobs SET Progress = @Progress, Heartbeat = @Heartbeat, Done = @Done WHERE Id = @Id;",
                        new
                        {
                            Id = transcodingJob.Id,
                            Progress = transcodingJob.Progress.TotalSeconds,
                            Heartbeat = DateTimeOffset.UtcNow.UtcDateTime,
                            Done = transcodingJob.Done
                        });

                    if (updatedRows != 1)
                        throw new Exception($"Failed to update progress for job id {transcodingJob.Id}");

                    FileInfo fileInfo = new FileInfo(jobRequest.DestinationFilename);
                    if (fileInfo.Exists == false &&
                        connection.Query<int>(
                            "SELECT COUNT(*) FROM FfmpegJobs WHERE JobCorrelationId = ? AND Done = 0;",
                            new {jobRequest.JobCorrelationId})
                            .Single() == 0)
                    {
                        var chunks = connection.Query<FfmpegPart>(
                            "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                            new {Id = transcodingJob.JobCorrelationId});

                        foreach (var chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
                        {
                            string fileNameWithoutExtension =
                                Path.GetFileNameWithoutExtension(jobRequest.DestinationFilename);
                            string fileExtension = Path.GetExtension(jobRequest.DestinationFilename);
                            string outputFolder = Path.GetDirectoryName(jobRequest.DestinationFilename);
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
                                "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration) VALUES(?, ?, ?, ?, ?);",
                                new
                                {
                                    transcodingJob.JobCorrelationId,
                                    arguments,
                                    jobRequest.Needed,
                                    jobRequest.VideoSourceFilename,
                                    duration
                                });
                        }
                    }

                    transaction.Commit();
                }
            }
        }
    }
}

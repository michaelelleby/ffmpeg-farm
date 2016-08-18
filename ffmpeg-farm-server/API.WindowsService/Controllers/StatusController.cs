using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using API.Service;
using Contract;
using Contract.Dto;
using Contract.Models;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        public JobStatusModel GetStatus()
        {
            return new JobStatusModel
            {
                Requests = GetJobStatuses()
            };
        }

        public JobStatusModel GetStatusForSpecificJob(Guid id)
        {
            if (id == Guid.Empty)
                throw new HttpResponseException(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.PreconditionFailed,
                    ReasonPhrase = "JobCorrelationId must be a valid GUID"
                });

            return
                null;
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

                JobRequest jobRequest = connection.Query<JobRequest>(
                    "SELECT JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, EnableDash, EnablePsnr FROM FfmpegRequest WHERE JobCorrelationId = @Id",
                    new {Id = job.JobCorrelationId})
                    .SingleOrDefault();
                if (jobRequest == null)
                    throw new ArgumentException($@"Job with correlation id {job.JobCorrelationId} not found");

                jobRequest.Targets = connection.Query<DestinationFormat>(
                    "SELECT JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate FROM FfmpegRequestTargets WHERE JobCorrelationId = @Id;",
                    new {Id = job.JobCorrelationId})
                    .ToArray();

                using (var scope = new TransactionScope())
                {
                    Type jobType = job.GetType();
                    TranscodingJobState jobState = job.Failed
                        ? TranscodingJobState.Failed
                        : job.Done
                            ? TranscodingJobState.Done
                            : TranscodingJobState.InProgress;

                    if (jobType == typeof(TranscodingJob))
                    {
                        int updatedRows = connection.Execute(
                            "UPDATE FfmpegJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
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
                            foreach (FfmpegPart chunk in ((TranscodingJob)job).Chunks)
                            {
                                connection.Execute(
                                    "UPDATE FFmpegParts SET PSNR = @Psnr WHERE Id = @Id;",
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
                            string tempFolder = string.Concat(Path.GetDirectoryName(jobRequest.DestinationFilename),
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

                using (var scope = new TransactionScope())
                {
                    ICollection<TranscodingJobState> totalJobs = connection.Query(
                            "SELECT State FROM FfmpegJobs WHERE JobCorrelationId = @Id;",
                            new { Id = jobRequest.JobCorrelationId })
                            .Select(x => (TranscodingJobState)Enum.Parse(typeof(TranscodingJobState), x.State))
                            .ToList();

                    if (totalJobs.Any(x => x != TranscodingJobState.Done))
                    {
                        // Not all transcoding jobs are finished
                        return;
                    }

                    totalJobs = connection.Query(
                        "SELECT State FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
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
                        QueueMergeJob(job, connection, outputFolder, fileNameWithoutExtension, fileExtension, jobRequest);
                    }
                    else if (jobRequest.EnableDash)
                    {
                        QueueMpegDashMergeJob(job, destinationFilename, connection, jobRequest, fileNameWithoutExtension, outputFolder, fileExtension);
                    }

                    scope.Complete();
                }
            }
        }

        private static IEnumerable<JobRequestModel> GetJobStatuses(Guid jobCorrelationId = default(Guid))
        {
            IEnumerable<TranscodingJobDto> jobs;
            IEnumerable<JobRequestDto> requests;
            using (var connection = Helper.GetConnection())
            {
                connection.Open();
                if (jobCorrelationId != default(Guid))
                {
                    requests =
                        connection.Query<JobRequestDto>("SELECT * from FfmpegRequest WHERE JobCorrelationId = @JobCorrelationId;",
                            new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                    jobs =
                        connection.Query<TranscodingJobDto>(
                            "SELECT * FROM FfmpegJobs WHERE JobCorrelationId = @JobCorrelationId;",
                            new {JobCorrelationId = jobCorrelationId})
                            .ToList();
                }
                else
                {
                    requests = connection.Query<JobRequestDto>("SELECT * from FfmpegRequest").ToList();
                    jobs = connection.Query<TranscodingJobDto>("SELECT * FROM FfmpegJobs").ToList();
                }
            }

            IEnumerable<JobRequestModel> requestModels = requests.Select(m => new JobRequestModel
            {
                JobCorrelationId = m.JobCorrelationId,
                VideoSourceFilename = m.VideoSourceFilename,
                AudioSourceFilename = m.AudioSourceFilename,
                DestinationFilename = m.DestinationFilename,
                Needed = m.Needed,
                Created = m.Created,
                MpegDash = m.EnableDash,
                Jobs = jobs.Where(x => x.JobCorrelationId == m.JobCorrelationId).Select(j => new TranscodingJobModel
                {
                    Progress = j.Progress,
                    Heartbeat = j.Heartbeat,
                    HeartbeatMachine = j.HeartBeatMachineName,
                    State = j.State,
                    ChunkDuration = j.ChunkDuration
                })
            });

            return requestModels;
        }

        private static void QueueMpegDashMergeJob(BaseJob job, string destinationFilename, IDbConnection connection,
            JobRequest jobRequest, string fileNameWithoutExtension, string outputFolder, string fileExtension)
        {
            string destinationFolder = Path.GetDirectoryName(destinationFilename);
            ICollection<TranscodingJobState> totalJobs =
                connection.Query("SELECT State FROM Mp4boxJobs WHERE JobCorrelationId = @Id;",
                    new {Id = jobRequest.JobCorrelationId})
                    .Select(x => (TranscodingJobState) Enum.Parse(typeof(TranscodingJobState), x.State))
                    .ToList();

            // One MPEG DASH merge job is already queued. Do nothing
            if (totalJobs.Any())
                return;

            string arguments =
                $@"-dash 4000 -rap -frag-rap -profile onDemand -out {destinationFolder}{Path.DirectorySeparatorChar}{fileNameWithoutExtension}.mpd";

            var chunks = connection.Query<FfmpegPart>(
                "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
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
                "SELECT Filename, Number, Target, (SELECT VideoSourceFilename FROM FfmpegRequest WHERE JobCorrelationId = @Id) AS VideoSourceFilename, Width, Height, Bitrate FROM FfmpegParts WHERE JobCorrelationId = @Id ORDER BY Target, Number;",
                new {Id = job.JobCorrelationId})
                .ToList();

            foreach (IEnumerable<FfmpegPart> chunk in chunks.GroupBy(x => x.Target, x => x, (key, values) => values))
            {
                var ffmpegParts = chunk as IList<FfmpegPart> ?? chunk.ToList();
                int targetNumber = ffmpegParts.First().Target;
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
                    foreach (FfmpegPart part in ffmpegParts.Where(x => x.IsAudio == false))
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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using API.Repository;
using API.Service;
using Contract;
using Contract.Dto;
using Contract.Models;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class StatusController : ApiController
    {
        private readonly IAudioJobRepository _repository;

        public StatusController(IAudioJobRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _repository = repository;
        }

        /// <summary>
        /// Get status for all jobs
        /// </summary>
        /// <returns></returns>
        public IEnumerable<JobRequestModel> Get()
        {
            IEnumerable<AudioJobRequestDto> jobStatuses = _repository.Get();
            IEnumerable<JobRequestModel> requestModels = jobStatuses.Select(m => new JobRequestModel
            {
                JobCorrelationId = m.JobCorrelationId,
                SourceFilename = m.SourceFilename,
                DestinationFilenamePrefix = m.DestinationFilename,
                Needed = m.Needed.GetValueOrDefault(),
                Created = m.Created,
                Jobs = m.Jobs.Select(j => new TranscodingJobModel
                {
                    Heartbeat = j.Heartbeat.GetValueOrDefault(),
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.State,
                })
            });

            return requestModels;
        }

        /// <summary>
        /// Get status for a specific job
        /// </summary>
        /// <param name="id">ID of job to get status of</param>
        /// <returns></returns>
        public JobRequestModel Get(Guid id)
        {
            if (id == Guid.Empty)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.PreconditionFailed, "ID must be a valid GUID"));

            AudioJobRequestDto request = _repository.Get(id);
            if (request == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No job found with id {id:B}"));

            JobRequestModel model = new JobRequestModel
            {
                JobCorrelationId = request.JobCorrelationId,
                SourceFilename = request.SourceFilename,
                DestinationFilenamePrefix = request.DestinationFilename,
                Needed = request.Needed.GetValueOrDefault(),
                Created = request.Created,
                OutputFolder = request.OutputFolder,
                Jobs = request.Jobs.Select(j => new TranscodingJobModel
                {
                    Heartbeat = j.Heartbeat.GetValueOrDefault(),
                    HeartbeatMachine = j.HeartbeatMachineName,
                    State = j.State
                })
            };


            return model;
        }

        /// <summary>
        /// Update status of an active job.
        /// 
        /// This also serves as a heartbeat, to tell the server
        /// that the client is still working actively on the job
        /// </summary>
        /// <param name="job"></param>
        public void Put(BaseJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.MachineName))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified"));

            Helper.InsertClientHeartbeat(job.MachineName);

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                int isVideo = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM FfmpegVideoRequest WHERE JobCorrelationId = @JobCorrelationId;",
                    new {job.JobCorrelationId});
                if (isVideo == 1)
                {
                    UpdateVideoJob(job, connection);
                    return;
                }

                int isAudio = connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM FfmpegAudioRequest WHERE JobCorrelationId = @JobCorrelationId;",
                    new { job.JobCorrelationId });
                if (isAudio == 1)
                {
                    UpdateAudioJob(job, connection);
                }
            }
        }

        private static void UpdateAudioJob(BaseJob job, IDbConnection  connection)
        {
            using (var scope = new TransactionScope())
            {
                TranscodingJobState jobState = job.Failed
                    ? TranscodingJobState.Failed
                    : job.Done
                        ? TranscodingJobState.Done
                        : TranscodingJobState.InProgress;

                int updatedRows = connection.Execute(
                    "UPDATE FfmpegAudioJobs SET Progress = @Progress, Heartbeat = @Heartbeat, State = @State, HeartbeatMachineName = @MachineName WHERE Id = @Id;",
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

                scope.Complete();
            }
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

            using (var scope = new TransactionScope())
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
                        "SELECT State FROM FfmpegVideoJobs WHERE JobCorrelationId = @Id;",
                        new {Id = jobRequest.JobCorrelationId})
                    .Select(x => (TranscodingJobState) Enum.Parse(typeof(TranscodingJobState), x.State))
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

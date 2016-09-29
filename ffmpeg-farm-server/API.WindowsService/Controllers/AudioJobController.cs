using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Service;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class AudioJobController : ApiController
    {
        private readonly IAudioJobRepository _repository;
        private readonly IHelper _helper;

        public AudioJobController(IAudioJobRepository repository, IHelper helper)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _repository = repository;
            _helper = helper;
        }

        public AudioTranscodingJob Get(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified"));

            _helper.InsertClientHeartbeat(machineName);

            return _repository.GetNextTranscodingJob();
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Guid Post(AudioJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            return HandleNewAudioJob(input);
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId">Job id returned when creating new job</param>
        public void Delete(Guid jobId)
        {
            if (jobId == Guid.Empty)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Job id must be a valid GUID."));

            bool deleteJob = _repository.DeleteJob(jobId, JobType.Audio);
            if (deleteJob == false)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Job {jobId:N} was not found"));
        }

        private Guid HandleNewAudioJob(AudioJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            AudioJobRequest jobRequest = new AudioJobRequest
            {
                Needed = request.Needed.LocalDateTime,
                Inpoint = request.Inpoint,
                Targets = request.Targets,
                SourceFilename = request.SourceFilename,
                OutputFolder = request.OutputFolder,
                DestinationFilename = request.DestinationFilenamePrefix
            };
            Guid jobCorrelationId = Guid.NewGuid();

            string sourceFilename = request.SourceFilename;

            var jobs = new List<AudioTranscodingJob>();
            foreach (var target in request.Targets)
            {
                AudioTranscodingJob transcodingJob = new AudioTranscodingJob
                {
                    JobCorrelationId = jobCorrelationId,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed.DateTime,
                    State = TranscodingJobState.Queued
                };

                string extension = ContainerHelper.GetExtension(target.Format);

                string destinationFilename =
                    $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{request.DestinationFilenamePrefix}_{target
                        .Bitrate}.{extension}";

                transcodingJob.Arguments =
                    $@"-y -i ""{sourceFilename}"" -c:a {target.AudioCodec.ToString().ToLowerInvariant()} -b:a {target
                        .Bitrate}k -vn ""{destinationFilename}""";

                jobs.Add(transcodingJob);
            }

            Directory.CreateDirectory(request.OutputFolder);
            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");

            return _repository.Add(jobRequest, jobs);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using API.Service;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class AudioJobController : ApiController
    {
        private readonly IAudioJobRepository _repository;

        public AudioJobController(IAudioJobRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _repository = repository;
        }

        public HttpResponseMessage Get(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified");
            }

            Helper.InsertClientHeartbeat(machineName);

            var job = _repository.GetNextTranscodingJob();

            return Request.CreateResponse(HttpStatusCode.OK, job);
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public HttpResponseMessage Post(AudioJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            return Request.CreateResponse(HttpStatusCode.Created, HandleNewAudioJob(input));
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId">Job id returned when creating new job</param>
        public HttpResponseMessage Delete(Guid jobId)
        {
            if (jobId == Guid.Empty)
                throw new ArgumentException("Job id must be a valid GUID.");

            return _repository.DeleteJob(jobId) == false
                ? Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Job {jobId:N} was not found")
                : Request.CreateResponse(HttpStatusCode.OK);
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
                DestinationFilename = request.DestinationFilenamePrefix
            };
            Guid jobCorrelationId = Guid.NewGuid();

            Directory.CreateDirectory(request.OutputFolder);

            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");

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

            return _repository.Add(jobRequest, jobs);
        }
    }
}
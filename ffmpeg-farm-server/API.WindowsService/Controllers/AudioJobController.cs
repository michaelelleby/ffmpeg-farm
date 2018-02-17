using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Database;
using API.Repository;
using API.Service;
using API.WindowsService.Models;
using Contract;
using Utils;

namespace API.WindowsService.Controllers
{
    [RoutePrefix("api/audiojob")]
    public class AudioJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly ILogging _logging;
        private readonly ApiSettings _settings;
        private readonly IGenerator _commandlineGenerator;
        private readonly IOutputFilenameGenerator _filenameGenerator;

        public AudioJobController(IHelper helper, ILogging logging, ApiSettings settings, IGenerator commandlineGenerator, IOutputFilenameGenerator filenameGenerator)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _commandlineGenerator = commandlineGenerator ?? throw new ArgumentNullException(nameof(commandlineGenerator));
            _filenameGenerator = filenameGenerator ?? throw new ArgumentNullException(nameof(filenameGenerator));
        }

        /// <summary>
        ///     Create a new job
        /// </summary>
        [HttpPost]
        [Route]
        public Guid CreateNew(AudioJobRequestModel input)
        {
            if (!ModelState.IsValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

            (FfmpegAudioRequest request, FfmpegJobs job) = HandleNewAudioJob(input);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.AudioRequests.Add(request);
                unitOfWork.Jobs.Add(job);

                unitOfWork.Complete();

                _logging.Info($"Created new audio job : {job.JobCorrelationId}");

                return job.JobCorrelationId;
            }
        }

        private (FfmpegAudioRequest, FfmpegJobs) HandleNewAudioJob(AudioJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Guid id = Guid.NewGuid();
            string sourceFilename = request.SourceFilenames.First();
            string uniqueNamePart = Guid.NewGuid().ToString(); //Used to avoid file collisions when transcoding the same file multiple times to the same location;

            int sourceDuration = _helper.GetDuration(sourceFilename);

            var jobs = new List<AudioTranscodingJob>();
            foreach (var target in request.Targets)
            {
                string extension = ContainerHelper.GetExtension(target.Format);
                string destinationFullPath = _filenameGenerator.Generate(request.DestinationFilenamePrefix, uniqueNamePart, target.Bitrate, extension, request.OutputFolder);
                var transcodingJob = new AudioTranscodingJob
                {
                    JobCorrelationId = id,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed,
                    State = TranscodingJobState.Queued,
                    OutputFilename = destinationFullPath,
                    Bitrate = target.Bitrate,
                    FfmpegCommandline = _commandlineGenerator.GenerateAudioCommandline(target, request.SourceFilenames, destinationFullPath),
                    ExpectedDuration = sourceDuration
                };

                jobs.Add(transcodingJob);
            }

            Directory.CreateDirectory(request.OutputFolder);
            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");
            
            var ffmpegrequest = new FfmpegAudioRequest
            {
                JobCorrelationId = id,
                Created = DateTimeOffset.UtcNow,
                DestinationFilename = request.DestinationFilenamePrefix,
                Needed = request.Needed,
                OutputFolder = request.OutputFolder,
                SourceFilename = string.Join(",", request.SourceFilenames)
            };
            ICollection<FfmpegTasks> tasks = jobs.Select(job => new FfmpegTasks
            {
                Arguments = job.FfmpegCommandline,
                DestinationDurationSeconds = job.ExpectedDuration,
                DestinationFilename = job.OutputFilename
            }).ToList();
            var ffmpegjob = new FfmpegJobs
            {
                Created = DateTimeOffset.UtcNow,
                JobCorrelationId = id,
                FfmpegTasks = tasks
            };

            return (ffmpegrequest, ffmpegjob);
        }
    }
}
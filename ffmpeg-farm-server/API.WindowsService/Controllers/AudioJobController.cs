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

namespace API.WindowsService.Controllers
{
    [RoutePrefix("api/audiojob")]
    public class AudioJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly ILogging _logging;
        private readonly ApiSettings _settings;

        public AudioJobController(IHelper helper, ILogging logging, ApiSettings settings)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

            var jobRequest = new AudioJobRequest
            {
                Needed = request.Needed.LocalDateTime,
                Inpoint = request.Inpoint,
                Targets = request.Targets,
                SourceFilenames = request.SourceFilenames,
                OutputFolder = request.OutputFolder,
                DestinationFilename = request.DestinationFilenamePrefix
            };
            var id = Guid.NewGuid();

            string sourceFilename = request.SourceFilenames.First();
            string uniqueNamePart = Guid.NewGuid().ToString(); //Used to avoid file collisions when transcoding the same file multiple times to the same location;

            int sourceDuration = _helper.GetDuration(sourceFilename);

            var jobs = new List<AudioTranscodingJob>();
            foreach (var target in request.Targets)
            {
                string extension = ContainerHelper.GetExtension(target.Format);

                string destinationFilename =
                    $@"{request.DestinationFilenamePrefix}_{uniqueNamePart}_{target.Bitrate}.{extension}";
                string destinationFullPath =
                    $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{destinationFilename}";
                ICollection<string> commandline = new List<string>();
                string outputFullPath = GetOutputFullPath(destinationFullPath);

                if (_settings.OverwriteOutput)
                    commandline.Add("-y");
                if (_settings.AbortOnError)
                    commandline.Add("-xerror");

                if (jobRequest.SourceFilenames.Count > 1)
                {
                    /*RESULT:
                     * -y -xerror
                     * -i "\\ondnas01\MediaCache\Test\test.mp3" -i "\\ondnas01\MediaCache\Test\radioavis.mp3" -i "\\ondnas01\MediaCache\Test\temp.mp3"
                     * -filter_complex
                     * [0:0][1:0][2:0]concat=n=3:a=1:v=0
                     * -c:a mp3 -b:a 64k -vn -map_metadata -1 -f MP3 \\ondnas01\MediaCache\Test\marvin\ffmpeg\test2.mp3
                    */
                    string streams = string.Empty;
                    var streamCount = 0;
                    foreach (string filename in jobRequest.SourceFilenames)
                    {
                        commandline.Add($@"-i ""{filename}""");
                        streams = $"{streams}[{streamCount++}:0]";
                    }

                    streams = $"{streams}concat=n={streamCount}:a=1:v=0";

                    commandline.Add($"-filter_complex {streams}");
                }
                else
                {
                    commandline.Add($@"-i ""{sourceFilename}""");
                }

                commandline.Add($"-c:a {target.AudioCodec.ToString().ToLower()}");
                commandline.Add($"-b:a {target.Bitrate}k");
                commandline.Add("-vn");

                if (target.Format == ContainerFormat.MP4)
                    commandline.Add("-movflags +faststart");

                commandline.Add("-map_metadata -1");
                commandline.Add($"-f {target.Format}");
                commandline.Add($@"""{outputFullPath}");

                var transcodingJob = new AudioTranscodingJob
                {
                    JobCorrelationId = id,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed,
                    State = TranscodingJobState.Queued,
                    OutputFilename = destinationFullPath,
                    Bitrate = target.Bitrate,
                    FfmpegCommandline = string.Join(" ", commandline),
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

        private string GetOutputFullPath(string destinationFullPath)
        {
            return _settings.TranscodeToLocalDisk ? @"|TEMP|" : destinationFullPath;
        }
    }
}
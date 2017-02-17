using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Service;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    /// <summary>
    /// Receives audio jobs orders.
    /// </summary>
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

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public Guid CreateNew(AudioJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            return HandleNewAudioJob(input);
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
                SourceFilenames = request.SourceFilenames,
                OutputFolder = request.OutputFolder,
                DestinationFilename = request.DestinationFilenamePrefix
            };
            Guid jobCorrelationId = Guid.NewGuid();

            string sourceFilename = request.SourceFilenames.First();
            string uniqueNamePart = Guid.NewGuid().ToString();//Used to avoid file collisions when transcoding the same file multiple times to the same location

            var frameCount = _helper.GetDuration(sourceFilename);

            var jobs = new List<AudioTranscodingJob>();
            foreach (var target in request.Targets)
            {
                string extension = ContainerHelper.GetExtension(target.Format);

                string destinationFilename = $@"{request.DestinationFilenamePrefix}_{uniqueNamePart}_{target.Bitrate}.{extension}";
                string destinationFullPath = $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{destinationFilename}";
                string arguments = string.Empty;
                string outputFullPath = Convert.ToBoolean(ConfigurationManager.AppSettings["TranscodeToLocalDisk"])
                    ? @"|TEMP|"
                    : destinationFullPath;

                if (jobRequest.SourceFilenames.Count == 1)
                {
                    if (target.Format == ContainerFormat.MP4)
                    {
                        arguments = $@"-y -xerror -i ""{sourceFilename}"" -c:a {target.AudioCodec.ToString().ToLowerInvariant()} -b:a {target
                                .Bitrate}k -vn -movflags +faststart -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    }
                    else
                    {
                        arguments = $@"-y -xerror -i ""{sourceFilename}"" -c:a {target.AudioCodec.ToString().ToLowerInvariant()} -b:a {target
                                .Bitrate}k -vn -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    }
                }
                else
                {
                    /*RESULT:
                     * -y -xerror
                     * -i "\\ondnas01\MediaCache\Test\test.mp3" -i "\\ondnas01\MediaCache\Test\radioavis.mp3" -i "\\ondnas01\MediaCache\Test\temp.mp3"
                     * -filter_complex
                     * [0:0][1:0][2:0]concat=n=3:a=1:v=0
                     * -c:a mp3 -b:a 64k -vn -map_metadata -1 -f MP3 \\ondnas01\MediaCache\Test\marvin\ffmpeg\test2.mp3
                    */
                    string filenameArguments = String.Empty, streams = String.Empty;
                    int streamCount = 0;
                    foreach (var filename in jobRequest.SourceFilenames)
                    {
                        filenameArguments += $@" -i ""{filename}"" ";
                        streams = $"{streams}[{streamCount++}:0]";
                    }

                    streams = $"{streams}concat=n={streamCount}:a=1:v=0";

                    if (target.Format == ContainerFormat.MP4)
                    {
                        arguments =
                            $@"-y -xerror{filenameArguments}-filter_complex {streams} -c:a {target.AudioCodec.ToString().ToLowerInvariant()} -b:a {target
                                .Bitrate}k -vn -movflags +faststart -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    }
                    else
                    {
                        arguments =
                            $@"-y -xerror{filenameArguments}-filter_complex {streams} -c:a {target.AudioCodec.ToString().ToLowerInvariant()} -b:a {target
                                .Bitrate}k -vn -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    }
                }

                var transcodingJob = new AudioTranscodingJob
                {
                    JobCorrelationId = jobCorrelationId,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed.DateTime,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = destinationFullPath,
                    Bitrate = target.Bitrate,
                    Arguments = arguments,
                    DestinationDurationSeconds = frameCount
                };

                jobs.Add(transcodingJob);
            }

            Directory.CreateDirectory(request.OutputFolder);
            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");

            return _repository.Add(jobRequest, jobs);
        }
    }
}
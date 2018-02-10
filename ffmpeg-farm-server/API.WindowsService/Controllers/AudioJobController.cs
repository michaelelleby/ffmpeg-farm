using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class AudioJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public AudioJobController(IHelper helper, ILogging logging)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _helper = helper;
            _logging = logging;
        }

        /// <summary>
        ///     Create a new job
        /// </summary>
        [HttpPost]
        public Guid CreateNew(AudioJobRequestModel input)
        {
            if (!ModelState.IsValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

            var res = HandleNewAudioJob(input);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.AudioRequests.Add(res.Item1);
                Guid jobId = unitOfWork.Jobs.Add(res.Item2).JobCorrelationId;

                unitOfWork.Complete();

                _logging.Info($"Created new audio job : {jobId}");

                return jobId;
            }
        }

        private Tuple<FfmpegAudioRequest, FfmpegJobs> HandleNewAudioJob(AudioJobRequestModel request)
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
            var jobCorrelationId = Guid.NewGuid();

            var sourceFilename = request.SourceFilenames.First();
            var uniqueNamePart =
                Guid.NewGuid()
                    .ToString(); //Used to avoid file collisions when transcoding the same file multiple times to the same location

            var frameCount = _helper.GetDuration(sourceFilename);

            var jobs = new List<AudioTranscodingJob>();
            foreach (var target in request.Targets)
            {
                var extension = ContainerHelper.GetExtension(target.Format);

                var destinationFilename =
                    $@"{request.DestinationFilenamePrefix}_{uniqueNamePart}_{target.Bitrate}.{extension}";
                var destinationFullPath = $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{destinationFilename}";
                string arguments;
                var outputFullPath = Convert.ToBoolean(ConfigurationManager.AppSettings["TranscodeToLocalDisk"])
                    ? @"|TEMP|"
                    : destinationFullPath;

                if (jobRequest.SourceFilenames.Count == 1)
                {
                    if (target.Format == ContainerFormat.MP4)
                        arguments = $@"-y -xerror -i ""{sourceFilename}"" -c:a {
                                target.AudioCodec.ToString().ToLowerInvariant()
                            } -b:a {
                                target
                                    .Bitrate
                            }k -vn -movflags +faststart -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    else
                        arguments = $@"-y -xerror -i ""{sourceFilename}"" -c:a {
                                target.AudioCodec.ToString().ToLowerInvariant()
                            } -b:a {
                                target
                                    .Bitrate
                            }k -vn -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
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
                    string filenameArguments = string.Empty, streams = string.Empty;
                    var streamCount = 0;
                    foreach (var filename in jobRequest.SourceFilenames)
                    {
                        filenameArguments += $@" -i ""{filename}"" ";
                        streams = $"{streams}[{streamCount++}:0]";
                    }

                    streams = $"{streams}concat=n={streamCount}:a=1:v=0";

                    if (target.Format == ContainerFormat.MP4)
                        arguments =
                            $@"-y -xerror{filenameArguments}-filter_complex {streams} -c:a {
                                    target.AudioCodec.ToString().ToLowerInvariant()
                                } -b:a {
                                    target
                                        .Bitrate
                                }k -vn -movflags +faststart -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
                    else
                        arguments =
                            $@"-y -xerror{filenameArguments}-filter_complex {streams} -c:a {
                                    target.AudioCodec.ToString().ToLowerInvariant()
                                } -b:a {
                                    target
                                        .Bitrate
                                }k -vn -map_metadata -1 -f {target.Format} ""{outputFullPath}""";
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
            
            var ffmpegrequest = new FfmpegAudioRequest
            {
                JobCorrelationId = jobCorrelationId,
                Created = DateTimeOffset.UtcNow,
                DestinationFilename = request.DestinationFilenamePrefix,
                Needed = request.Needed,
                OutputFolder = request.OutputFolder,
                SourceFilename = string.Join(",", request.SourceFilenames)
            };
            ICollection<FfmpegTasks> tasks = jobs.Select(j => new FfmpegTasks
            {
                Arguments = j.Arguments,
                DestinationDurationSeconds = j.DestinationDurationSeconds,
                DestinationFilename = j.DestinationFilename
            }).ToList();
            var ffmpegjob = new FfmpegJobs
            {
                Created = DateTimeOffset.UtcNow,
                JobCorrelationId = jobCorrelationId,
                FfmpegTasks = tasks
            };

            return Tuple.Create(ffmpegrequest, ffmpegjob);
        }
    }
}
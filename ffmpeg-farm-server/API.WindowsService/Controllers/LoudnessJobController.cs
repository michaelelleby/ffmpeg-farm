using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using API.Service;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class LoudnessJobController : ApiController
    {
        private readonly ILoudnessJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public LoudnessJobController(ILoudnessJobRepository repository, IHelper helper, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _helper = helper;
            _logging = logging;
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public Guid CreateNew(LoudnessJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewLoudnessJob(input);
            _logging.Info($"Created new loudness job : {res}");
            return res;
        }

        private Guid HandleNewLoudnessJob(LoudnessJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jobRequest = new LoudnessJobRequest
            {
                Needed = request.Needed.LocalDateTime,
                SourceFilenames = request.SourceFilenames,
                OutputFolder = request.OutputFolder,
                DestinationFilename = request.DestinationFilename
            };
            Guid jobCorrelationId = Guid.NewGuid();

            string sourceFilename = request.SourceFilenames.First();
            string uniqueNamePart = Guid.NewGuid().ToString(); //Used to avoid file collisions when transcoding the same file multiple times to the same location

            var frameCount = _helper.GetDuration(sourceFilename);

            var jobs = new List<LoudnessJob>();
            {
                if (request.OutputFolder.EndsWith(@"\"))
                    request.OutputFolder = request.OutputFolder.Remove(request.OutputFolder.LastIndexOf(@"\", StringComparison.Ordinal), 1);
                string destinationFullPath = $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{request.DestinationFilename}";
                string arguments = string.Empty;
                string outputFullPath = Convert.ToBoolean(ConfigurationManager.AppSettings["TranscodeToLocalDisk"])
                    ? @"|TEMP|"
                    : destinationFullPath;

                if (jobRequest.SourceFilenames.Count == 1)
                {
                    //When piping from ffmpeg to stereotool the output file will not have its chunksize header set correctly. We fix this by piping stereotool output through ffmpeg again with no options, that seems to fix the file. If the wav header is not correct ffmpeg wont transcode the wav file.
                    //ffmpeg decompress -> stereotool -> ffmpeg headerfix
                    arguments = $"{{FFMpegPath}} -xerror -i \"{sourceFilename}\" -f wav -hide_banner -loglevel info - | \"{{StereoToolPath}}\" - - -s \"{{StereoToolPresetsPath}}{Path.DirectorySeparatorChar}{request.AudioPresetFile}\"\"{{StereoToolLicense}}\" -q | {{FFMpegPath}} -i pipe:0 \"{outputFullPath}\"";
                }
                else
                {
                    /*RESULT:
                     * -xerror
                     * -i "\\ondnas01\MediaCache\Test\test.mp3" -i "\\ondnas01\MediaCache\Test\radioavis.mp3" -i "\\ondnas01\MediaCache\Test\temp.mp3"
                     * -filter_complex
                     * [0:0][1:0][2:0]concat=n=3:a=1:v=0
                     * -f wav -hide_banner -loglevel info - |
                     * "{StereoToolPath}" - "\\ondnas01\MediaCache\Test\marvin\ffmpeg\test2.wav"
                     * -s "{StereoToolPresetsPath}\presetfilename.sts"
                     * -k "{StereoToolLicense}"
                     * -q
                    */
                    string filenameArguments = String.Empty, streams = String.Empty;
                    int streamCount = 0;
                    foreach (var filename in jobRequest.SourceFilenames)
                    {
                        filenameArguments += $@" -i ""{filename}"" ";
                        streams = $"{streams}[{streamCount++}:0]";
                    }

                    streams = $"{streams}concat=n={streamCount}:a=1:v=0";

                    arguments = $"{{FFMpegPath}} -xerror{filenameArguments}-filter_complex {streams} -f wav -hide_banner -loglevel info - | \"{{StereoToolPath}}\" - - -s \"{{StereoToolPresetsPath}}{Path.DirectorySeparatorChar}{request.AudioPresetFile}\"\"{{StereoToolLicense}}\" -q | {{FFMpegPath}} -i pipe:0 \"{outputFullPath}\"";
                }

                var loudnessJob = new LoudnessJob
                {
                    JobCorrelationId = jobCorrelationId,
                    SourceFilename = sourceFilename,
                    Needed = request.Needed.DateTime,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = destinationFullPath,
                    Arguments = arguments,
                    DestinationDurationSeconds = frameCount
                };

                jobs.Add(loudnessJob);
            }

            Directory.CreateDirectory(request.OutputFolder);
            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");

            return _repository.Add(jobRequest, jobs);
        }
    }
}

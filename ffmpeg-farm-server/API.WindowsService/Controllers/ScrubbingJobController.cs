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
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class ScrubbingJobController : ApiController
    {
        private readonly IScrubbingJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public ScrubbingJobController(IScrubbingJobRepository repository, IHelper helper, ILogging logging)
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
        public Guid CreateNew(ScrubbingJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewScrubbingJob(input);
            _logging.Info($"Created new scrubbing job : {res}");
            return res;
        }

        private Guid HandleNewScrubbingJob(ScrubbingJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var jobRequest = new ScrubbingJobRequest
            {
                SourceFilename = request.SourceFilename,
                Needed = request.Needed.LocalDateTime,
                OutputFolder = request.OutputFolder,
                FirstThumbnailOffsetInSeconds = request.FirstThumbnailOffsetInSeconds,
                MaxSecondsBetweenThumbnails = request.MaxSecondsBetweenThumbnails,
                SpriteSheetSizes = request.SpriteSheetSizes.ConvertAll(x => (SpriteSheetSize) Enum.Parse(typeof(SpriteSheetSize), x)),
                ThumbnailResolutions = request.ThumbnailResolutions
            };
            Guid jobCorrelationId = Guid.NewGuid();
            string sourceFilename = jobRequest.SourceFilename;

            var jobs = new List<ScrubbingJob>();
            var info = _helper.GetMediainfo(sourceFilename);
            if (info.Duration <= 0)
                throw new InvalidDataException($"Scrubbing request failed. Input file is invalid. Duration: {info.Duration} sec.");

            foreach (var spriteSheetSize in jobRequest.SpriteSheetSizes)
            {
                int hTiles, vTiles;
                var numberOfThumbnailsPerFile = spriteSheetSize.SpriteSheetTiles(out hTiles, out vTiles);
                var framesBetweenDumps = calculateFramesBetweenDumps(info.Duration, numberOfThumbnailsPerFile, info.Framerate, jobRequest.MaxSecondsBetweenThumbnails, jobRequest.FirstThumbnailOffsetInSeconds);
                string fps = (int)Math.Round(info.Framerate) + "/" + framesBetweenDumps; //ie 25/250 which is the same as 1/10

                foreach (var resolution in jobRequest.ThumbnailResolutions)
                {
                    var outputBaseFilename = $"{Path.GetFileNameWithoutExtension(sourceFilename)}-{resolution.Replace(":","x")}-{spriteSheetSize.ToString()}";
                    var offset = TimeSpan.FromSeconds(jobRequest.FirstThumbnailOffsetInSeconds);

                    var outputThumbFile = $"{jobRequest.OutputFolder}{Path.DirectorySeparatorChar}{outputBaseFilename}-%03d.jpg";
                    var arguments = $"-ss {offset} -loglevel info -i \"{sourceFilename}\" -y -vf \"fps={fps},scale={resolution},tile={hTiles}x{vTiles}\" \"{outputThumbFile}\"";

                    var scrubbingJob = new ScrubbingJob
                    {
                        JobCorrelationId = jobCorrelationId,
                        SourceFilename = sourceFilename,
                        Needed = request.Needed.DateTime,
                        State = TranscodingJobState.Queued,
                        DestinationFilename = outputThumbFile,
                        Arguments = arguments,
                    };
                    jobs.Add(scrubbingJob);

                    var fileNumber = 1;
                    var keepGoing = true;
                    while (keepGoing)
                    {
                        // Are we done?
                        if (framesBetweenDumps * numberOfThumbnailsPerFile * fileNumber >= (info.Duration - jobRequest.FirstThumbnailOffsetInSeconds) * info.Framerate)
                            keepGoing = false;
                        else
                        {
                            offset = offset.Add(TimeSpan.FromSeconds(numberOfThumbnailsPerFile * (framesBetweenDumps / info.Framerate)));
                            fileNumber++;
                        }
                    }

                    var webVttFile = $"{jobRequest.OutputFolder}{Path.DirectorySeparatorChar}{outputBaseFilename}.vtt";
                    generateWebVtt(webVttFile, fileNumber, outputBaseFilename, spriteSheetSize, framesBetweenDumps, jobRequest.FirstThumbnailOffsetInSeconds, (int) info.Framerate, (int) (info.Duration*1000), resolution);

                }
            }

            return _repository.Add(jobRequest, jobs);
        }



        private int calculateFramesBetweenDumps(double videoDurationInSeconds, int numberOfThumbnails, double videoFramesPerSecond, int maxSecondsBetweenDumps, int firstThumbOffsetInSeconds)
        {
            int framesBetweenDumps = (int) Math.Ceiling(((videoDurationInSeconds - firstThumbOffsetInSeconds) / numberOfThumbnails * videoFramesPerSecond));
            if (framesBetweenDumps > (maxSecondsBetweenDumps * videoFramesPerSecond))
                framesBetweenDumps = (int) (maxSecondsBetweenDumps * videoFramesPerSecond);
            if (framesBetweenDumps < videoFramesPerSecond)
                framesBetweenDumps = (int)videoFramesPerSecond;
            return framesBetweenDumps;
        }

        private void generateWebVtt(string webVttFile, int numberOfFiles, string thumbBaseFilename, SpriteSheetSize spriteSheetSize, int framesBetweenDumps, int firstThumbnailOffsetInSeconds, int framePerSecond, int videoDurationInMilliseconds, string resolution)
        {
            int hTiles, vTiles;
            var numberOfThumbnailsPerFile = spriteSheetSize.SpriteSheetTiles(out hTiles, out vTiles);
            int thumbWidth, thumbHeight;
            getThumbWidthAndHeight(resolution, out thumbWidth, out thumbHeight);

            var content = "WEBVTT ";
            var curThumb = 0;
            var curThumbInFile = 0;
            var curFileNumber = 1;
            var keepGoing = true;
            var firstThumb = true;
            var curStartTimeMillisecond = 0;
            var millisecondsBetweenDumps = (int)Math.Round(((double)framesBetweenDumps / (double)framePerSecond) * 1000);
            var curEndTimeMillisecond = firstThumbnailOffsetInSeconds * 1000 + millisecondsBetweenDumps;
            while (keepGoing)
            {
                for (var v = 0; v < vTiles; v++)
                {
                    for (var h = 0; h < hTiles; h++)
                    {
                        var outputThumbFile = $"{thumbBaseFilename}-{curFileNumber:D3}.jpg";

                        if (!firstThumb)
                        {
                            curStartTimeMillisecond = curEndTimeMillisecond; // Start where last one ended.
                            curEndTimeMillisecond = curEndTimeMillisecond + millisecondsBetweenDumps; // Add time = frames between each dump.
                        }

                        firstThumb=false;
                        curThumb++;
                        curThumbInFile++;

                        if (curThumbInFile == numberOfThumbnailsPerFile)
                        {
                            // We've reached end-of-thumbnail-file.
                            curThumbInFile = 0;

                            if (curFileNumber == numberOfFiles)
                            {
                                // Last thumb of last file? Set end-time to video duration.
                                curEndTimeMillisecond = videoDurationInMilliseconds;
                                keepGoing = false;
                            }
                            else
                                curFileNumber++;
                        }

                        var nextEndTimeSecond = curEndTimeMillisecond + millisecondsBetweenDumps;
                        if (nextEndTimeSecond > videoDurationInMilliseconds)
                        {
                            // There are not enough seconds in next segment for a screendump - therefore extend current period to end-of-videofile.
                            curEndTimeMillisecond = videoDurationInMilliseconds;
                            keepGoing = false;
                        }

                        if (curStartTimeMillisecond < videoDurationInMilliseconds)
                        {
                            content += $"{getTimeSpanString(curStartTimeMillisecond)} --> {getTimeSpanString(curEndTimeMillisecond)}{Environment.NewLine}";
                            //content += $"{outputThumbFile}#xywh={h * thumbWidth},{v * thumbHeight},{((h + 1) * thumbWidth)-1},{((v + 1) * thumbHeight)-1}";
                            content += $"{outputThumbFile}#xywh={h * thumbWidth},{v * thumbHeight},{thumbWidth},{thumbHeight}";
                        }
                        else
                            keepGoing = false; // we shouldn't end up here - but just in case...


                        if (keepGoing)
                            // Don't add 2 newlines in the very end.
                            content += $"{Environment.NewLine}{Environment.NewLine}";
                        else
                            break;
                    }

                    if (!keepGoing)
                        break;
                }
            }

            File.WriteAllText(webVttFile, content, Encoding.UTF8);
            /*
            WEBVTT 00:00:00.000 --> 00:00:05.000
            thumbnails-001.jpg#xywh=0,0,160,90

            00:00:05.000 --> 00:00:10.000
            thumbnails-001.jpg#xywh=160,0,160,90

            00:00:10.000 --> 00:00:15.000
            thumbnails-001.jpg#xywh=0,90,160,90

            00:00:15.000 --> 00:00:20.000
            thumbnails-001.jpg#xywh=160,90,160,90
            */
        }

        private string getTimeSpanString(int millisec)
        {
            return $"{TimeSpan.FromMilliseconds(millisec):hh\\:mm\\:ss\\.fff}";
        }

        private void getThumbWidthAndHeight(string resolution, out int width, out int height)
        {
            width = int.Parse(resolution.Substring(0, resolution.IndexOf(":", StringComparison.Ordinal)));
            height = int.Parse(resolution.Substring(resolution.IndexOf(":", StringComparison.Ordinal) + 1));
        }

    }
}

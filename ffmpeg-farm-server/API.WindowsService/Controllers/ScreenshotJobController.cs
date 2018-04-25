using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class ScreenshotJobController : ApiController
    {
        private readonly IScreenshotJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;
        private readonly int _defaultScreenshotWidth = 1280;
        private readonly int _defaultScreenshotHeight = 720;

        public ScreenshotJobController(IScreenshotJobRepository repository, IHelper helper, ILogging logging)
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
        public Guid CreateNew(ScreenshotJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewScreenshotJob(input);
            _logging.Info($"Created new audio job : {res}");
            return res;
        }

        private Guid HandleNewScreenshotJob(ScreenshotJobRequestModel request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            ScreenshotJobRequest jobRequest = new ScreenshotJobRequest
            {
                Needed = request.Needed.LocalDateTime,
                VideoSourceFilename = request.VideoSourceFilename,
                OutputFolder = request.OutputFolder,
                DestinationFilename = request.DestinationFilename,
                ScreenshotTime = request.ScreenshotTime,
                Height = request.Height,
                Width = request.Width,
                AspectRatio16_9 = request.AspectRatio16_9
            };
        
            string outputFilePath = Path.Combine(request.OutputFolder, $"{request.DestinationFilename}.jpg");

            string ffmpegArguments = string.Format("-ss {1} -i \"{0}\" -vframes 1 -deinterlace -s {3}x{4} -an -y -v 0 -f image2 \"{2}\"", request.VideoSourceFilename, request.ScreenshotTime, outputFilePath, _defaultScreenshotWidth, _defaultScreenshotHeight);

            // force_original_aspect_ratio=decrease will cause FFmpeg to generate black bars in the side of the generated screenshot
            // for 4:3 videos, to avoid stretching the original picture from the video,
            if (!request.AspectRatio16_9)
            {
                ffmpegArguments =
                    string.Format(
                        "-ss {1} -i \"{0}\" -vframes 1 -filter_complex \"yadif=0:-1:0,scale={3}:{4}:force_original_aspect_ratio=decrease,pad={3}:{4}:(ow-iw)/2:(oh-ih)/2\" -an -y -v 0 -f image2 \"{2}\"",
                        request.VideoSourceFilename, request.ScreenshotTime, outputFilePath, _defaultScreenshotWidth, _defaultScreenshotHeight);
            }

            var job = new ScreenshotJob
            {
                Arguments = ffmpegArguments,
                DestinationFilename = outputFilePath
            };

            var jobs = new List<ScreenshotJob>{ job };
            return _repository.Add(jobRequest, jobs);
        }
    }
}
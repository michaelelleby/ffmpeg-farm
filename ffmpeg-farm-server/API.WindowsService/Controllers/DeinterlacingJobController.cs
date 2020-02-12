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
    public class DeinterlacingJobController : ApiController
    {
        private readonly IDeinterlacingJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public DeinterlacingJobController(IDeinterlacingJobRepository repository, IHelper helper, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _helper = helper;
            _logging = logging;
        }

        [HttpPost]
        public Guid CreateNew(DeinterlacingJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewDeinterlacingJob(input);
            _logging.Info($"Created new deinterlacing job : {res}");
            return res;
        }

        private Guid HandleNewDeinterlacingJob(DeinterlacingJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            int frameCount = _helper.GetDuration(model.VideoSourceFilename);

            string arguments = string.Empty;
            
            arguments += $@"-xerror -i ""{model.VideoSourceFilename}"" -vf yadif=1 -c:v mpeg2video -preset medium -c:a copy -map 0 -b:v 50000k -movflags +faststart -y ""{outputFilename}""";
            var jobs = new List<FFmpegJob>
            {
                new DeinterlacingJob
                {
                    Arguments = arguments,
                    FfmpegExePath = ConfigurationWrapper.FFmpeg341,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = outputFilename,
                    DestinationDurationSeconds = frameCount
                }
            };
            var request = new DeinterlacingJobRequest
            {
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return _repository.Add(request, jobs);
        }
    }
}
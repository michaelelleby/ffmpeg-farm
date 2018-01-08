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
    public class AudioDemuxJobController : ApiController
    {
        private readonly IAudioDemuxJobRepository _repository;
        private readonly ILogging _logging;

        public AudioDemuxJobController(IAudioDemuxJobRepository repository, ILogging logging)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        [HttpPost]
        public Guid CreateNew(AudioDemuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res = HandleNewAudioDemuxJob(input);
            _logging.Info($"Created new mux job : {res}");
            return res;
        }

        private Guid HandleNewAudioDemuxJob(AudioDemuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";

            string arguments = string.Empty;

            //TODO: Fix the ffmpeg args so the job will work
            arguments += $"-i {model.VideoSourceFilename} {outputFilename} -y";
            var jobs = new List<FFmpegJob>
            {
                new AudioDemuxJob
                {
                    Needed = model.Needed.LocalDateTime,
                    Arguments = arguments,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = outputFilename
                }
            };
            var request = new AudioDemuxJobRequest
            {
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return _repository.Add(request, jobs);
        }
    }
}
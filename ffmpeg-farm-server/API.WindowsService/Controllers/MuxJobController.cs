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
    public class MuxJobController : ApiController
    {
        private readonly IMuxJobRepository _repository;

        public MuxJobController(IMuxJobRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _repository = repository;
        }

        [HttpPost]
        public Guid CreateNew(MuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            return HandleNewMuxJob(input);
        }

        private Guid HandleNewMuxJob(MuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";

            string arguments = string.Empty;
            if (model.Inpoint > TimeSpan.Zero)
            {
                arguments += $"-ss {model.Inpoint:g} ";
            }
            arguments += $@"-i ""{model.VideoSourceFilename}"" -i ""{model.AudioSourceFilename}"" -map 0:v:0 -map 1:a:0 -c copy -y ""{outputFilename}""";
            var jobs = new List<FFmpegJob>
            {
                new MuxJob
                {
                    Arguments = arguments,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = outputFilename
                }
            };
            var request = new MuxJobRequest
            {
                AudioSourceFilename = model.AudioSourceFilename,
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return _repository.Add(request, jobs);
        }
    }
}
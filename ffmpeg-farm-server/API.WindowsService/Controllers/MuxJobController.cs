using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using API.Database;
using API.Repository;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class MuxJobController : ApiController
    {
        private readonly IHelper _helper;

        public MuxJobController(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _helper = helper;
        }

        [HttpPost]
        public Guid CreateNew(MuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                var job = HandleNewMuxJob(input);
                
                unitOfWork.MuxRequests.Add(job.Item2);
                return unitOfWork.Jobs.Add(job.Item1).JobCorrelationId;
            }
        }

        private Tuple<FfmpegJobs, FfmpegMuxRequest> HandleNewMuxJob(MuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            var frameCount = _helper.GetDuration(model.VideoSourceFilename);

            string arguments = string.Empty;
            if (model.Inpoint > TimeSpan.Zero)
            {
                arguments += $"-ss {model.Inpoint:g} ";
            }
            arguments += $@"-xerror -i ""{model.VideoSourceFilename}"" -i ""{model.AudioSourceFilename}"" -map 0:v:0 -map 1:a:0 -c copy -y ""{outputFilename}""";
            var jobs = new FfmpegJobs
            {
                JobCorrelationId = Guid.NewGuid(),
                Created = DateTimeOffset.UtcNow,
                Needed = model.Needed,
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = arguments,
                        TaskState = TranscodingJobState.Queued,
                        DestinationFilename = outputFilename,
                        DestinationDurationSeconds = frameCount
                    }
                }
            };
            var request = new FfmpegMuxRequest
            {
                AudioSourceFilename = model.AudioSourceFilename,
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return Tuple.Create(jobs, request);
        }
    }
}
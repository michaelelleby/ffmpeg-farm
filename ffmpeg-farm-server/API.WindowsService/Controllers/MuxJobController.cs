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
    [RoutePrefix("api/muxjob")]
    public class MuxJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly IApiSettings _settings;

        public MuxJobController(IHelper helper, IApiSettings settings)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [Route]
        [HttpPost]
        public Guid CreateNew(MuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            (FfmpegJobs job, FfmpegMuxRequest request) = HandleNewMuxJob(input);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.MuxRequests.Add(request);
                unitOfWork.Jobs.Add(job);

                unitOfWork.Complete();
            }

            return job.JobCorrelationId;
        }

        private (FfmpegJobs, FfmpegMuxRequest) HandleNewMuxJob(MuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            ICollection<string> commandline = new List<string>();
            if (_settings.OverwriteOutput)
                commandline.Add("-y");
            if (_settings.AbortOnError)
                commandline.Add("-xerror");

            string outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            int frameCount = _helper.GetDuration(model.VideoSourceFilename);

            if (model.Inpoint > TimeSpan.Zero)
            {
                commandline.Add($"-ss {model.Inpoint:g}");
            }
            commandline.Add($@"-i ""{model.VideoSourceFilename}""");
            commandline.Add($@"-i ""{model.AudioSourceFilename}""");
            commandline.Add("-map 0:v:0");
            commandline.Add("-map 1:a:0");
            commandline.Add("-c copy");
            commandline.Add($@"""{outputFilename}""");

            FfmpegJobs jobs = new FfmpegJobs
            {
                JobCorrelationId = Guid.NewGuid(),
                Created = DateTimeOffset.UtcNow,
                Needed = model.Needed,
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = string.Join(" ", commandline),
                        TaskState = TranscodingJobState.Queued,
                        DestinationFilename = outputFilename,
                        DestinationDurationSeconds = frameCount
                    }
                }
            };
            FfmpegMuxRequest request = new FfmpegMuxRequest
            {
                AudioSourceFilename = model.AudioSourceFilename,
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return (jobs, request);
        }
    }
}
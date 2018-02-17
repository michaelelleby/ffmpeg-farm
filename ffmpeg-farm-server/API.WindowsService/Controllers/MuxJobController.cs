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
        private readonly IGenerator _commandlineGenerator;

        public MuxJobController(IHelper helper, IApiSettings settings, IGenerator commandlineGenerator)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _commandlineGenerator = commandlineGenerator ?? throw new ArgumentNullException(nameof(commandlineGenerator));
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

            string outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            int frameCount = _helper.GetDuration(model.VideoSourceFilename);

            FfmpegJobs jobs = new FfmpegJobs
            {
                JobCorrelationId = Guid.NewGuid(),
                Created = DateTimeOffset.UtcNow,
                Needed = model.Needed,
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = _commandlineGenerator.GenerateMuxCommandline(model.VideoSourceFilename, model.AudioSourceFilename, outputFilename, model.Inpoint),
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
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
    public class AudioDemuxJobController : ApiController
    {
        private readonly ILogging _logging;
        private readonly IApiSettings _settings;

        public AudioDemuxJobController(ILogging logging, IApiSettings settings)
        {
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [HttpPost]
        public Guid CreateNew(AudioDemuxJobRequestModel input)
        {
            if (!ModelState.IsValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

            var (request, job) = HandleNewAudioDemuxJob(input);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.MuxRequests.Add(request);
                unitOfWork.Jobs.Add(job);

                unitOfWork.Complete();
            }

            _logging.Info($"Created new mux job : {job.JobCorrelationId}");

            return job.JobCorrelationId;
        }

        private (FfmpegMuxRequest, FfmpegJobs) HandleNewAudioDemuxJob(AudioDemuxJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            string outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";

            ICollection<string> commandline = new List<string>();
            if (_settings.OverwriteOutput)
                commandline.Add("-y");
            if (_settings.AbortOnError)
                commandline.Add("-xerror");

            //TODO: Fix the ffmpeg args so the job will work
            commandline.Add($@"-i ""{model.VideoSourceFilename}""");
            commandline.Add($@"""{outputFilename}""");

            var jobs = new FfmpegJobs
            {
                Needed = model.Needed.LocalDateTime,
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = string.Join(" ", commandline),
                        TaskState = TranscodingJobState.Queued,
                        DestinationFilename = outputFilename
                    }
                },
                JobCorrelationId = Guid.NewGuid()
            };
            var request = new FfmpegMuxRequest
            {
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder,
                JobCorrelationId = jobs.JobCorrelationId
            };

            return (request, jobs);
        }
    }
}
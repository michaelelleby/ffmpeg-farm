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
        private readonly IAudioDemuxJobRepository _repository;
        private readonly ILogging _logging;

        public AudioDemuxJobController(IAudioDemuxJobRepository repository, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _logging = logging;
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

            var jobs = new FfmpegJobs()
            {
                Needed = model.Needed.LocalDateTime,
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = arguments,
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


            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.MuxRequests.Add(request);
                unitOfWork.Jobs.Add(jobs);

                unitOfWork.Complete();
            }

            return jobs.JobCorrelationId;
        }
    }
}
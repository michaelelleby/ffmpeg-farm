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
    [RoutePrefix("api/hardsubtitlesjob")]
    public class HardSubtitlesJobController : ApiController
    {
        private readonly IHelper _helper;
        private readonly ILogging _logging;
        private readonly IApiSettings _settings;

        public HardSubtitlesJobController(IHelper helper, ILogging logging, IApiSettings settings)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [Route]
        [HttpPost]
        public Guid CreateNew(HardSubtitlesJobRequestModel input)
        {
            if (!ModelState.IsValid)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));

            var (request, job) = HandleNewHardSubtitlesJob(input);

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                unitOfWork.HardSubtitlesRequest.Add(request);
                unitOfWork.Jobs.Add(job);

                unitOfWork.Complete();
            }

            _logging.Info($"Created new hard sub job : {job.JobCorrelationId}");

            return job.JobCorrelationId;
        }

        private (HardSubtitlesJobRequest, FfmpegJobs) HandleNewHardSubtitlesJob(HardSubtitlesJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            string outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            int frameCount = _helper.GetDuration(model.VideoSourceFilename);

            ICollection<string> commandline = new List<string>();
            if (_settings.OverwriteOutput)
                commandline.Add("-y");
            if (_settings.AbortOnError)
                commandline.Add("-xerror");

            if (model.Inpoint > TimeSpan.Zero)
                commandline.Add($"-ss {model.Inpoint:g}");

            commandline.Add($@"-i ""{model.VideoSourceFilename}""");
            commandline.Add($@"-filter_complex ""subtitles='{model.SubtitlesFilename.Replace("\\", "\\\\")}':force_style='{_helper.HardSubtitlesStyle()}'""");
            commandline.Add("-preset ultrafast");
            commandline.Add("-c:v mpeg4");
            commandline.Add("-b:v 50M");
            commandline.Add("-c:a copy");
            commandline.Add($@"""{outputFilename}""");

            var jobs = new FfmpegJobs
            {
                FfmpegTasks = new List<FfmpegTasks>
                {
                    new FfmpegTasks
                    {
                        Arguments = string.Join(" ", commandline),
                        TaskState = TranscodingJobState.Queued,
                        DestinationFilename = outputFilename,
                        DestinationDurationSeconds = frameCount
                    }
                },
                JobCorrelationId = Guid.NewGuid()
            };
            var request = new HardSubtitlesJobRequest
            {
                SubtitlesFilename = model.SubtitlesFilename,
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return (request, jobs);
        }
    }
}
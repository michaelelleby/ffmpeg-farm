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
    public class HardSubtitlesJobController : ApiController
    {
        private readonly IHardSubtitlesJobRepository _repository;
        private readonly IHelper _helper;
        private readonly ILogging _logging;

        public HardSubtitlesJobController(IHardSubtitlesJobRepository repository, IHelper helper, ILogging logging)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _repository = repository;
            _helper = helper;
            _logging = logging;
        }

        [HttpPost]
        public Guid CreateNew(HardSubtitlesJobRequestModel input)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState));
            }

            var res= HandleNewHardSubtitlesxJob(input);
            _logging.Info($"Created new hard sub job : {res}");
            return res;
        }

        private Guid HandleNewHardSubtitlesxJob(HardSubtitlesJobRequestModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var outputFilename = $"{model.OutputFolder}{Path.DirectorySeparatorChar}{model.DestinationFilename}";
            var frameCount = _helper.GetDuration(model.VideoSourceFilename);

            string arguments = string.Empty;
            if (model.Inpoint > TimeSpan.Zero)
            {
                arguments += $"-ss {model.Inpoint:g} ";
            }

            string audioFilter = "";
            string audioMap = $"-map 0:{model.LeftStream}";
            if (model.LeftStream != model.RightStream)
            {
                //merge two audio streams into one stereostream
                audioFilter = ";[0:1][0:2]amerge=inputs=2[aout]";
                audioMap = "-map \"[aout]\"";
            }

            string outputCodec = "mpeg2video";
            if (!string.IsNullOrEmpty(model.CodecId) && model.CodecId.ToLower().Contains("dv")) 
            {
                // Only transcode to mpeg4 if origin is DVCPRO. DVCPRO to mpeg2video fails for some reason.
                outputCodec = "mpeg4";
            }

            arguments += $@"-xerror -i ""{model.VideoSourceFilename}"" -filter_complex ""subtitles='{model.SubtitlesFilename.Replace("\\", "\\\\")}':force_style='{_helper.HardSubtitlesStyle()}'{audioFilter}"" -preset ultrafast -c:v {outputCodec} -b:v 50M {audioMap} -y ""{outputFilename}""";

            var jobs = new List<FFmpegJob>
            {
                new HardSubtitlesJob()
                {
                    Arguments = arguments,
                    Needed = model.Needed,
                    State = TranscodingJobState.Queued,
                    DestinationFilename = outputFilename,
                    DestinationDurationSeconds = frameCount
                }
            };
            var request = new HardSubtitlesJobRequest()
            {
                SubtitlesFilename = model.SubtitlesFilename,
                VideoSourceFilename = model.VideoSourceFilename,
                DestinationFilename = model.DestinationFilename,
                OutputFolder = model.OutputFolder
            };

            return _repository.Add(request, jobs);
        }
    }
}
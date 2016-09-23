using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Transactions;
using System.Web.Http;
using API.Service;
using API.WindowsService.Models;
using Contract;

namespace API.WindowsService.Controllers
{
    public class VideoJobController : ApiController
    {
        private readonly IVideoJobRepository _repository;

        public VideoJobController(IVideoJobRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            _repository = repository;
        }

        /// <summary>
        /// Get next transcoding job
        /// </summary>
        /// <param name="machineName">Client's machine name used to stamp who took the job</param>
        /// <returns><see cref="TranscodingJob"/></returns>
        public HttpResponseMessage Get(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machinename must be specified");
            }

            Helper.InsertClientHeartbeat(machineName);

            Mp4boxJob dashJob = _repository.GetNextDashJob();
            if (dashJob != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, dashJob);
            }

            var job = _repository.GetNextMergeJob() ?? _repository.GetNextTranscodingJob();
            return Request.CreateResponse(HttpStatusCode.OK, job);
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId">Job id returned when creating new job</param>
        [HttpDelete]
        public HttpResponseMessage Delete(Guid jobId)
        {
            if (jobId == Guid.Empty)
                throw new ArgumentException("Job id must be a valid GUID.");

            return _repository.DeleteJob(jobId) == false
                ? Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Job {jobId:N} was not found")
                : Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        /// Queue new transcoding job
        /// </summary>
        /// <param name="job"></param>
        public HttpResponseMessage Post(VideoJobRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            var jobId = HandleNewVideoJob(request);

            return Request.CreateResponse(HttpStatusCode.Created, $"{jobId:N}");
        }

        
        private Guid HandleNewVideoJob(VideoJobRequestModel request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string extension = ContainerHelper.GetExtension(request.ContainerFormat);
            JobRequest job = new JobRequest
            {
                Needed = request.Needed.LocalDateTime,
                AudioSourceFilename = request.AudioSourceFilename,
                EnableDash = request.EnableMpegDash,
                EnablePsnr = request.EnablePsnr,
                EnableTwoPass = request.EnableTwoPass,
                Inpoint = request.Inpoint,
                Targets = request.Targets,
                VideoSourceFilename = request.SourceFilename,
                X264Preset = request.FFmpegPreset,
                DestinationFilename = Path.Combine(request.DestinationFilenamePrefix, extension)
            };
            Mediainfo mi = Helper.GetMediainfo(job.VideoSourceFilename);
            Guid jobCorrelationId = Guid.NewGuid();

            Directory.CreateDirectory(request.OutputFolder);

            if (!Directory.Exists(request.OutputFolder))
                throw new ArgumentException($@"Destination folder {request.OutputFolder} does not exist.");

            ICollection<VideoTranscodingJob> transcodingJobs = new List<VideoTranscodingJob>();
            const int chunkDuration = 60;

            // Queue audio first because it cannot be chunked and thus will take longer to transcode
            // and if we do it first chances are it will be ready when all the video parts are ready
            string source = !string.IsNullOrWhiteSpace(job.AudioSourceFilename)
                ? job.AudioSourceFilename
                : job.VideoSourceFilename;

            int t = 0;
            foreach (DestinationFormat format in job.Targets)
            {
                format.Target = t++;
            }

            VideoTranscodingJob audioJob = new VideoTranscodingJob
            {
                JobCorrelationId = jobCorrelationId,
                SourceFilename = source,
                Needed = job.Needed,
                State = TranscodingJobState.Queued
            };
            StringBuilder arguments = new StringBuilder($@"-y -ss {job.Inpoint} -i ""{source}""");
            //for (int i = 0; i < job.Targets.Length; i++)
            foreach (int bitrate in job.Targets.Select(x => x.AudioBitrate).Distinct())
            {
                string chunkFilename =
                    $@"{request.OutputFolder}{Path.DirectorySeparatorChar}{request.DestinationFilenamePrefix}_{bitrate}_audio.{0}";
                arguments.Append($@" -c:a aac -b:a {bitrate}k -vn ""{chunkFilename}""");

                audioJob.Chunks.Add(
                    new FfmpegPart
                    {
                        SourceFilename = source,
                        JobCorrelationId = jobCorrelationId,
                        Filename = chunkFilename,
                        Target = 0,
                        Number = 0
                    });
            }
            audioJob.Arguments = new[] {arguments.ToString()};

            transcodingJobs.Add(audioJob);

            IList<Resolution> resolutions =
                job.Targets.GroupBy(x => new {x.Width, x.Height}).Select(x => new Resolution
                {
                    Width = x.Key.Width,
                    Height = x.Key.Height,
                    Bitrates = x.Select(format => new Quality
                    {
                        VideoBitrate = format.VideoBitrate,
                        AudioBitrate = format.AudioBitrate,
                        Level = string.IsNullOrWhiteSpace(format.Level) ? "3.1" : format.Level.Trim(),
                        Profile = format.Profile,
                        Target = format.Target
                    }),
                }).ToList();

            int duration = Convert.ToInt32(mi.Duration - job.Inpoint.GetValueOrDefault().TotalSeconds);
            for (int i = 0; duration - i*chunkDuration > 0; i++)
            {
                int value = i*chunkDuration;
                if (value > duration)
                {
                    value = duration;
                }

                var transcodingJob = TranscodingJob(request, value, chunkDuration, resolutions, jobCorrelationId, mi, request.OutputFolder, request.DestinationFilenamePrefix,
                    extension, i, job.Inpoint.GetValueOrDefault());

                transcodingJobs.Add(transcodingJob);
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    _repository.SaveJobs(job, transcodingJobs, connection, jobCorrelationId, chunkDuration);

                    scope.Complete();

                    return jobCorrelationId;
                }
            }
        }

        private static VideoTranscodingJob TranscodingJob(VideoJobRequestModel job, int value, int chunkDuration, IList<Resolution> resolutions,
            Guid jobCorrelationId, Mediainfo mi, string destinationFolder, string destinationFilenamePrefix, string extension, int i,
            TimeSpan inpoint)
        {
            var argumentList = new List<string>();
            var arguments = new StringBuilder();
            var transcodingJob = new VideoTranscodingJob
            {
                JobCorrelationId = jobCorrelationId,
                SourceFilename = job.SourceFilename,
                Needed = job.Needed,
                State = TranscodingJobState.Queued,
            };
            string x264Preset = string.IsNullOrWhiteSpace(job.FFmpegPreset) ? "medium" : job.FFmpegPreset.ToLowerInvariant().Trim();
            int refs = 8388608 / (mi.Width * mi.Height);

            if (job.EnableTwoPass)
            {
                arguments.Append(
                    $@"-y -ss {inpoint + TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.SourceFilename}"" -filter_complex ""yadif=0:-1:0,format=yuv420p,");

                arguments.Append($"split={resolutions.Count}");
                for (int j = 0; j < resolutions.Count; j++)
                {
                    arguments.Append($"[in{j}]");
                }
                arguments.Append(";");

                for (int j = 0; j < resolutions.Count; j++)
                {
                    arguments.Append($"[in{j}]scale={resolutions[j].Width}:{resolutions[j].Height}:sws_flags=lanczos[out{j}];");
                }
                arguments = arguments.Remove(arguments.Length - 1, 1);
                // Remove trailing semicolon, ffmpeg does not like a semicolon after the last filter
                arguments.Append(@"""");

                for (int j = 0; j < resolutions.Count; j++)
                {
                    Resolution resolution = resolutions[j];
                    string chunkPassFilename =
                        $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{resolution
                            .Width}x{resolution.Height}_{value}.stats";

                    arguments.Append(
                        $@" -map [out{j}] -pass 1 -profile {resolution.Bitrates.First().Profile} -passlogfile ""{chunkPassFilename}"" -an -c:v libx264 -refs {refs} -psy 0 -aq-mode 0 -me_method tesa -me_range 16 -preset {x264Preset} -aspect 16:9 -f mp4 NUL");
                }

                argumentList.Add(arguments.ToString());
            }

            arguments.Clear();
            arguments.Append($@"-y -ss {inpoint + TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.SourceFilename}"" -filter_complex ""yadif=0:-1:0,format=yuv420p,");

            arguments.Append($"split={resolutions.Count}");
            for (int j = 0; j < resolutions.Count; j++)
            {
                arguments.Append($"[in{j}]");
            }
            arguments.Append(";");

            for (int j = 0; j < resolutions.Count; j++)
            {
                arguments.Append(
                    $"[in{j}]scale={resolutions[j].Width}:{resolutions[j].Height}:sws_flags=lanczos,split={resolutions[j].Bitrates.Count()}");

                for (int k = 0; k < resolutions[j].Bitrates.Count(); k++)
                {
                    arguments.Append($"[out{j}_{k}]");
                }
                arguments.Append(";");
            }
            arguments = arguments.Remove(arguments.Length - 1, 1);
            // Remove trailing semicolon, ffmpeg does not like a semicolon after the last filter
            arguments.Append(@"""");

            for (int j = 0; j < resolutions.Count; j++)
            {
                Resolution resolution = resolutions[j];
                for (int k = 0; k < resolution.Bitrates.Count(); k++)
                {
                    Quality quality = resolution.Bitrates.ToList()[k];
                    string chunkFilename =
                        $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{resolution
                            .Width}x{resolution.Height}_{quality.VideoBitrate}_{quality.AudioBitrate}_{value}{extension}";

                    arguments.Append($@" -map [out{j}_{k}] -an -c:v libx264 -refs {refs} -psy 0 -aq-mode 0 -me_method tesa -me_range 16 -b:v {quality.VideoBitrate}k -profile:v {quality.Profile} -level {quality.Level} -preset {x264Preset} -aspect 16:9 ");
                    if (job.EnableTwoPass)
                    {
                        string chunkPassFilename =
                        $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{resolution
                            .Width}x{resolution.Height}_{value}.stats";
                        arguments.Append($@"-pass 2 -passlogfile ""{chunkPassFilename}"" ");
                    }

                    if (job.EnablePsnr)
                    {
                        arguments.Append("-psnr ");
                    }

                    arguments.Append($@"""{chunkFilename}""");

                    transcodingJob.Chunks.Add(new FfmpegPart
                    {
                        JobCorrelationId = jobCorrelationId,
                        SourceFilename = job.SourceFilename,
                        Filename = chunkFilename,
                        Target = quality.Target,
                        Number = i
                    });
                }

            }

            argumentList.Add(arguments.ToString());
            transcodingJob.Arguments = argumentList.ToArray();

            return transcodingJob;
        }
    }
}
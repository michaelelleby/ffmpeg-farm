using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Transactions;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;

namespace API.WindowsService.Controllers
{
    public class TranscodingJobController : ApiController
    {
        /// <summary>
        /// Get next transcoding job
        /// </summary>
        /// <param name="machineName">Client's machine name used to stamp who took the job</param>
        /// <returns><see cref="TranscodingJob"/></returns>
        public BaseJob GetNextJob(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                throw new HttpResponseException(new HttpResponseMessage
                {
                    ReasonPhrase = "Machinename must be specified",
                    StatusCode = HttpStatusCode.BadRequest
                });
            }

            Helper.InsertClientHeartbeat(machineName);

            Mp4boxJob dashJob = GetNextDashJob();
            if (dashJob != null)
                return dashJob;

            return GetNextMergeJob() ?? GetTranscodingJob();
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        /// <param name="jobId">Job id returned when creating new job</param>
        [HttpDelete]
        public void Delete(Guid jobId)
        {
            if (jobId == Guid.Empty)
                throw new ArgumentException("Job id must be a valid GUID.");

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    connection.Execute("DELETE FROM FfmpegJobs WHERE JobCorrelationId = @Id;", new { Id = jobId });

                    connection.Execute("DELETE FROM FfmpegParts WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FfmpegMergeJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM Mp4boxJobs WHERE JobCorrelationId = @Id;", new {Id = jobId});

                    connection.Execute("DELETE FROM FFmpegRequestTargets WHERE JobCorrelationId = @Id;",
                        new {Id = jobId});

                    int rowsDeleted = connection.Execute("DELETE FROM FfmpegRequest WHERE JobCorrelationId = @Id;",
                        new { Id = jobId });
                    if (rowsDeleted != 1)
                        throw new ArgumentException($@"No job with id {jobId} found.");

                    scope.Complete();
                }
            }
        }

        private static MergeJob GetNextMergeJob()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
                    DateTime timeout = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

                    var data = connection.Query<dynamic>(
                        "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegMergeJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                        new
                        {
                            QueuedState = TranscodingJobState.Queued,
                            InProgressState = TranscodingJobState.InProgress,
                            Heartbeat = timeout
                        })
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated = connection.Execute(
                        "UPDATE FfmpegMergeJobs SET State = @State, HeartBeat = @Heartbeat WHERE Id = @Id;",
                        new {State = TranscodingJobState.InProgress, Heartbeat = DateTime.UtcNow, Id = data.Id});
                    if (rowsUpdated == 0)
                    {
                        throw new Exception("Failed to mark row as taken");
                    }

                    scope.Complete();

                    return new MergeJob
                    {
                        Id = Convert.ToInt32(data.Id),
                        Arguments = new string[] { data.Arguments},
                        JobCorrelationId = data.JobCorrelationId
                    };
                }
            }
        }

        private static TranscodingJob GetTranscodingJob()
        {
            int timeoutSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutSeconds"]);
            DateTime timeout =
                DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(timeoutSeconds));

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    var data = connection.Query<dynamic>(
                        "SELECT TOP 1 Id, Arguments, JobCorrelationId FROM FfmpegJobs WHERE State = @QueuedState OR (State = @InProgressState AND HeartBeat < @Heartbeat) ORDER BY Needed ASC, Id ASC;",
                        new
                        {
                            QueuedState = TranscodingJobState.Queued,
                            InProgressState = TranscodingJobState.InProgress,
                            Heartbeat = timeout
                        })
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var parts = connection.Query<dynamic>("SELECT Id, JobCorrelationId, Filename, Number, Target, PSNR FROM FfmpegParts WHERE FfmpegJobs_Id = @JobId;",
                        new {JobId = data.Id});
                    
                    var rowsUpdated =
                        connection.Execute(
                            "UPDATE FfmpegJobs SET State = @State, HeartBeat = @Heartbeat, Started = @Heartbeat WHERE Id = @Id;",
                            new {State = TranscodingJobState.InProgress, Heartbeat = DateTime.UtcNow, Id = data.Id});
                    if (rowsUpdated == 0)
                    {
                        throw new Exception("Failed to mark row as taken");
                    }

                    scope.Complete();

                    var job = new TranscodingJob
                    {
                        Id = Convert.ToInt32(data.Id),
                        Arguments = data.Arguments.Split('|'),
                        JobCorrelationId = data.JobCorrelationId,
                        Chunks = parts.Select(x => new FfmpegPart
                        {
                            Id = x.Id,
                            JobCorrelationId = x.JobCorrelationId,
                            Psnr = x.PSNR,
                            Target = x.Target,
                            Number = x.Number,
                            SourceFilename = x.SourceFilename,
                            Filename = x.Filename,
                        }).ToList()
                    };
                    return job;
                }
            }
        }

        private static Mp4boxJob GetNextDashJob()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    var data = connection.Query<Mp4boxJob>(
                        "SELECT TOP 1 JobCorrelationId, Arguments FROM Mp4boxJobs WHERE State = @State ORDER BY Needed ASC, Id ASC;",
                        new {State = TranscodingJobState.Queued})
                        .SingleOrDefault();
                    if (data == null)
                    {
                        return null;
                    }

                    var rowsUpdated =
                        connection.Execute("UPDATE Mp4boxJobs SET State = @State WHERE JobCorrelationId = @Id;",
                            new {State = TranscodingJobState.InProgress, Id = data.JobCorrelationId});
                    if (rowsUpdated != 1)
                    {
                        return null;
                    }

                    scope.Complete();

                    return new Mp4boxJob
                    {
                        JobCorrelationId = data.JobCorrelationId,
                        Arguments = data.Arguments
                    };
                }
            }
        }

        /// <summary>
        /// Queue new transcoding job
        /// </summary>
        /// <param name="job"></param>
        public Guid PostQueueNew(JobRequest job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(job.VideoSourceFilename) && string.IsNullOrWhiteSpace(job.AudioSourceFilename))
                throw new ArgumentException("Either VideoSourceFilename or AudioSourceFilename is a required parameter.");
            if (!string.IsNullOrWhiteSpace(job.VideoSourceFilename) && !File.Exists(job.VideoSourceFilename))
                throw new FileNotFoundException("VideoSourceFilename does not exist", job.VideoSourceFilename);
            if (!string.IsNullOrWhiteSpace(job.AudioSourceFilename) && !File.Exists(job.AudioSourceFilename))
                throw new FileNotFoundException("AudioSourceFilename does not exist", job.AudioSourceFilename);

            Mediainfo mi = Helper.GetMediainfo(job.VideoSourceFilename);
            Guid jobCorrelationId = Guid.NewGuid();

            string destinationFormat = Path.GetExtension(job.DestinationFilename);
            string destinationFolder = string.Concat(Path.GetDirectoryName(job.DestinationFilename),
                Path.DirectorySeparatorChar, jobCorrelationId.ToString("N"));
            string destinationFilenamePrefix = Path.GetFileNameWithoutExtension(job.DestinationFilename);

            Directory.CreateDirectory(destinationFolder);

            if (string.IsNullOrWhiteSpace(destinationFormat))
                throw new ArgumentException("DestinationFilename must have an extension to determine the output format.");

            if (!Directory.Exists(destinationFolder))
                throw new ArgumentException($@"Destination folder {destinationFolder} does not exist.");

            ICollection<TranscodingJob> transcodingJobs = new List<TranscodingJob>();
            const int chunkDuration = 60;

            // Queue audio first because it cannot be chunked and thus will take longer to transcode
            // and if we do it first chances are it will be ready when all the video parts are ready
            string source = job.HasAlternateAudio
                ? job.AudioSourceFilename
                : job.VideoSourceFilename;

            int t  = 0;
            foreach (DestinationFormat format in job.Targets)
            {
                format.Target = t++;
            }

            TranscodingJob audioJob = new TranscodingJob
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
                    $@"{destinationFolder}{Path.DirectorySeparatorChar}{destinationFilenamePrefix}_{bitrate}_audio.mp4";
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
            audioJob.Arguments = new []{arguments.ToString()};

            transcodingJobs.Add(audioJob);

            int target = 0;
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

                var transcodingJob = TranscodingJob(job, value, chunkDuration, resolutions, jobCorrelationId, mi, destinationFolder, destinationFilenamePrefix, destinationFormat, i, job.Inpoint.GetValueOrDefault());

                transcodingJobs.Add(transcodingJob);
            }

            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    SaveJobs(job, transcodingJobs, connection, jobCorrelationId, chunkDuration);

                    scope.Complete();

                    return jobCorrelationId;
                }
            }
        }

        private static TranscodingJob TranscodingJob(JobRequest job, int value, int chunkDuration, IList<Resolution> resolutions,
            Guid jobCorrelationId, Mediainfo mi, string destinationFolder, string destinationFilenamePrefix, string destinationFormat, int i,
            TimeSpan inpoint)
        {
            var argumentList = new List<string>();
            var arguments = new StringBuilder();
            var transcodingJob = new TranscodingJob
            {
                JobCorrelationId = jobCorrelationId,
                SourceFilename = job.VideoSourceFilename,
                Needed = job.Needed,
                State = TranscodingJobState.Queued,
            };
            string x264Preset = string.IsNullOrWhiteSpace(job.X264Preset) ? "medium" : job.X264Preset.Trim();
            int refs = 8388608 / (mi.Width * mi.Height);

            if (job.EnableTwoPass)
            {
                arguments.Append(
                    $@"-y -ss {inpoint + TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.VideoSourceFilename}"" -filter_complex ""yadif=0:-1:0,format=yuv420p,");

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
            arguments.Append($@"-y -ss {inpoint + TimeSpan.FromSeconds(value)} -t {chunkDuration} -i ""{job.VideoSourceFilename}"" -filter_complex ""yadif=0:-1:0,format=yuv420p,");

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
                            .Width}x{resolution.Height}_{quality.VideoBitrate}_{quality.AudioBitrate}_{value}{destinationFormat}";

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
                        SourceFilename = job.VideoSourceFilename,
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

        private static void SaveJobs(JobRequest job, ICollection<TranscodingJob> jobs, IDbConnection connection,
            Guid jobCorrelationId, int chunkDuration)
        {
            if (jobs.Any(x => x.State == TranscodingJobState.Unknown))
                throw new ArgumentException(
                    "One or more jobs have state TranscodingJobState.Unknown. A valid state must be set before saving to database");

            connection.Execute(
                "INSERT INTO FfmpegRequest (JobCorrelationId, VideoSourceFilename, AudioSourceFilename, DestinationFilename, Needed, Created, EnableDash, EnableTwoPass, EnablePsnr) VALUES(@JobCorrelationId, @VideoSourceFilename, @AudioSourceFilename, @DestinationFilename, @Needed, @Created, @EnableDash, @EnableTwoPass, @EnablePsnr);",
                new
                {
                    JobCorrelationId = jobCorrelationId,
                    job.VideoSourceFilename,
                    job.AudioSourceFilename,
                    job.DestinationFilename,
                    job.Needed,
                    Created = DateTime.UtcNow,
                    job.EnableDash,
                    job.EnableTwoPass, job.EnablePsnr
                });

            foreach (DestinationFormat target in job.Targets)
            {
                connection.Execute(
                    "INSERT INTO FfmpegRequestTargets (JobCorrelationId, Width, Height, VideoBitrate, AudioBitrate, H264Level, H264Profile) VALUES(@JobCorrelationId, @Width, @Height, @VideoBitrate, @AudioBitrate, @Level, @Profile);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Width = target.Width,
                        Height = target.Height,
                        VideoBitrate = target.VideoBitrate,
                        AudioBitrate = target.AudioBitrate,
                        Level = target.Level,
                        Profile = target.Profile
                    });
            }

            foreach (TranscodingJob transcodingJob in jobs)
            {
                connection.Execute(
                    "INSERT INTO FfmpegJobs (JobCorrelationId, Arguments, Needed, VideoSourceFilename, ChunkDuration, State) VALUES(@JobCorrelationId, @Arguments, @Needed, @VideoSourceFilename, @ChunkDuration, @State);",
                    new
                    {
                        JobCorrelationId = jobCorrelationId,
                        Arguments = string.Join("|", transcodingJob.Arguments),
                        Needed = transcodingJob.Needed,
                        VideoSourceFilename = transcodingJob.SourceFilename,
                        ChunkDuration = chunkDuration,
                        State = transcodingJob.State
                    });

                int jobId = connection.Query<int>("SELECT @@IDENTITY;")
                    .Single();

                foreach (FfmpegPart part in transcodingJob.Chunks)
                {
                    DestinationFormat format = job.Targets[part.Target];
                    connection.Execute(
                        "INSERT INTO FfmpegParts (JobCorrelationId, Target, Filename, Number, FfmpegJobs_Id, Width, Height, Bitrate) VALUES(@JobCorrelationId, @Target, @Filename, @Number, @FfmpegJobsId, @Width, @Height, @Bitrate);",
                        new
                        {
                            JobCorrelationId = jobCorrelationId,
                            Target = part.Target,
                            Filename = part.Filename,
                            Number = part.Number,
                            FfmpegJobsId = jobId,
                            Width = format.Width,
                            Height = format.Height,
                            Bitrate = format.VideoBitrate
                        });
                }
            }
        }
    }
}
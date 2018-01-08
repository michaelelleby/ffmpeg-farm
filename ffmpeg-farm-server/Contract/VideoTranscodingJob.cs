using System;
using System.Collections.Generic;

namespace Contract
{
    public class VideoTranscodingJob : BaseJob
    {
        /// <summary>
        /// Commandline arguments sent to FFmpeg
        /// </summary>
        public string[] Arguments { get; set; }

        public ICollection<FfmpegPart> Chunks { get; set; }
        public DateTimeOffset Needed { get; set; }

        public VideoTranscodingJob()
        {
            Chunks = new List<FfmpegPart>();
        }

        public new JobType Type => JobType.Video;
    }
}
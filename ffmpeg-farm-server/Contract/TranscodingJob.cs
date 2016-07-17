using System;
using System.Collections.Generic;

namespace Contract
{
    public class TranscodingJob : BaseJob
    {
        public TranscodingJobState State { get; set; }

        /// <summary>
        /// Source filename
        /// </summary>
        public string SourceFilename { get; set; }

        /// <summary>
        /// Commandline arguments sent to FFmpeg
        /// </summary>
        public string[] Arguments { get; set; }

        public ICollection<FfmpegPart> Chunks { get; set; }
        public DateTime Needed { get; set; }

        public TranscodingJob()
        {
            Chunks = new List<FfmpegPart>();
        }
    }
}
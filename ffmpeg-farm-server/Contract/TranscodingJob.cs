using System;

namespace Contract
{
    public class TranscodingJob : BaseJob
    {
        /// <summary>
        /// Source filename
        /// </summary>
        public string SourceFilename { get; set; }

        /// <summary>
        /// Commandline arguments sent to FFmpeg
        /// </summary>
        public string Arguments { get; set; }
    }
}
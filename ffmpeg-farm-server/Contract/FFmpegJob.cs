using System;

namespace Contract
{
    public class FFmpegJob : BaseJob
    {
        public string Arguments { get; set; }
        public DateTimeOffset Needed { get; set; }
        public string DestinationFilename { get; set; }
        public virtual JobType Type { get; }
    }
}
using System;

namespace Contract
{
    public class FFmpegJob : BaseJob
    {
        public DateTimeOffset Needed { get; set; }
        public string DestinationFilename { get; set; }
        public virtual JobType Type { get; }
        public int DestinationDurationSeconds { get; set; }
    }
}
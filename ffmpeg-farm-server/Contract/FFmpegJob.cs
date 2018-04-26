using System;

namespace Contract
{
    public class FFmpegJob : BaseJob
    {
        public string FfmpegExePath { get; set; }
        public string Arguments { get; set; }
        public DateTimeOffset Needed { get; set; }
        public string DestinationFilename { get; set; }
        public int DestinationDurationSeconds { get; set; }
    }
}
using System;

namespace Contract
{
    public abstract class FFmpegJob : BaseJob
    {
        public string FfmpegCommandline { get; set; }
        public DateTimeOffset Needed { get; set; }
        public string OutputFilename { get; set; }
        public int ExpectedDuration { get; set; }
    }
}
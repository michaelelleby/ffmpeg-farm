using System;

namespace Contract
{
    public class AudioTranscodingJob : BaseJob
    {
        public string Arguments { get; set; }

        public DateTimeOffset Needed { get; set; }

        public new JobType Type => JobType.Audio;
    }
}
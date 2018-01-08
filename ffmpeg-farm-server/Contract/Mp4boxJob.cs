using System;

namespace Contract
{
    public class Mp4boxJob : BaseJob
    {
        public string Arguments { get; set; }

        public new JobType Type => JobType.VideoMp4box;
    }
}
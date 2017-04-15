using System;

namespace Contract
{
    public class Mp4boxJob : BaseJob
    {
        public new JobType Type => JobType.VideoMp4box;
    }
}
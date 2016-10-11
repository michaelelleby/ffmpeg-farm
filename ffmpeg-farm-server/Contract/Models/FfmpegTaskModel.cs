using System;

namespace Contract.Models
{
    public class FfmpegTaskModel
    {
        public float Progress { get; set; }

        public TranscodingJobState State { get; set; }

        public DateTimeOffset? Heartbeat { get; set; }

        public string HeartbeatMachine { get; set; }
    }
}
using System;

namespace Contract.Models
{
    public class TranscodingJobModel
    {
        public float Progress { get; set; }
        public string HeartbeatMachine { get; set; }
        public DateTimeOffset Heartbeat { get; set; }
        public TranscodingJobState State { get; set; }
        public DateTimeOffset Needed { get; set; }
        public int ChunkDuration { get; set; }
    }
}
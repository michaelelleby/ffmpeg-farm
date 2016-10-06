using System;

namespace Contract
{
    public class AudioTranscodingJobDto
    {
        public Guid JobCorrelationId { get; set; }

        public string Arguments { get; set; }

        public DateTimeOffset? Needed { get; set; }

        public string SourceFilename { get; set; }

        public TranscodingJobState State { get; set; }

        public DateTimeOffset? Started { get; set; }

        public DateTimeOffset? Heartbeat { get; set; }

        public string HeartbeatMachineName { get; set; }

        public string DestinationFilename { get; set; }

        public int Bitrate { get; set; }
    }
}
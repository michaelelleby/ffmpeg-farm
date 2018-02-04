using System;
using API.Database;

namespace Contract.Dto
{
    public class TranscodingJobDto
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public float Progress { get; set; }

        public DateTimeOffset Heartbeat { get; set; }

        public string Arguments { get; set; }

        public DateTimeOffset Needed { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        public int ChunkDuration { get; set; }

        public string HeartBeatMachineName { get; set; }

        public TranscodingJobState State { get; set; }

        public DateTimeOffset Started { get; set; }
    }
}
using System;

namespace Contract.Dto
{
    public class TranscodingJobDto : BaseJob
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public float Progress { get; set; }

        public DateTimeOffset Needed { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        public int ChunkDuration { get; set; }

        public DateTimeOffset Started { get; set; }
    }
}
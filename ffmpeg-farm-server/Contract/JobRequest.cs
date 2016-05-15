using System;

namespace Contract
{
    public class JobRequest
    {
        public Guid JobCorrelationId { get; set; }

        public string SourceFilename { get; set; }

        public string DestinationFilename { get; set; }

        public DateTime Needed { get; set; }

        public DestinationFormat[] Targets { get; set; }
    }
}
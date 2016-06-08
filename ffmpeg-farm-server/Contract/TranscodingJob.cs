using System;

namespace Contract
{
    public class TranscodingJob
    {
        public Guid JobCorrelationId { get; set; }
        public string SourceFilename { get; set; }
        public string Arguments { get; set; }
        public TimeSpan Progress { get; set; }
        public int Id { get; set; }
        public bool Done { get; set; }
        public string MachineName { get; set; }
    }
}
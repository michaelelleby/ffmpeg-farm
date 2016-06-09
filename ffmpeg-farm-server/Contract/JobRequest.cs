using System;

namespace Contract
{
    public class JobRequest
    {
        public Guid JobCorrelationId { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        public string DestinationFilename { get; set; }

        public DateTime Needed { get; set; }

        public DestinationFormat[] Targets { get; set; }

        public bool HasAlternateAudio => !string.IsNullOrWhiteSpace(AudioSourceFilename) && !string.IsNullOrWhiteSpace(VideoSourceFilename);
    }
}
using System;

namespace Contract
{
    public class AudioJobRequest
    {
        public string SourceFilename { get; set; }

        public TimeSpan Inpoint { get; set; }

        public AudioDestinationFormat[] Targets { get; set; }

        public DateTimeOffset Needed { get; set; }

        public string DestinationFilename { get; set; }

        public string OutputFolder { get; set; }
    }
} 
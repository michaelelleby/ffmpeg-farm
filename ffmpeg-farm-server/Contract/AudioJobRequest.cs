using System;

namespace Contract
{
    public class AudioJobRequest : JobRequestBase
    {
        public string SourceFilename { get; set; }

        public TimeSpan Inpoint { get; set; }

        public AudioDestinationFormat[] Targets { get; set; }
    }
} 
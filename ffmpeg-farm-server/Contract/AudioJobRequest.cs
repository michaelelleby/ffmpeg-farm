using System;
using System.Collections.Generic;

namespace Contract
{
    public class AudioJobRequest : JobRequestBase
    {
        public List<string> SourceFilenames { get; set; }

        public TimeSpan Inpoint { get; set; }

        public AudioDestinationFormat[] Targets { get; set; }
    }
} 
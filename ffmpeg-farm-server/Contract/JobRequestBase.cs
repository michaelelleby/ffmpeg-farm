using System;

namespace Contract
{
    public class JobRequestBase
    {
        public DateTimeOffset Needed { get; set; }
        public string DestinationFilename { get; set; }
        public string OutputFolder { get; set; }
    }
}
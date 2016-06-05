using System;

namespace Contract
{
    public class FfmpegPart
    {
        public string SourceFilename { get; set; }

        public string Filename { get; set; }

        public int Number { get; set; }

        public int Target { get; set; }

        public bool IsAudio => Filename.EndsWith("_audio.mp4", StringComparison.InvariantCultureIgnoreCase);

        public Guid JobCorrelationId { get; set; }
    }
}
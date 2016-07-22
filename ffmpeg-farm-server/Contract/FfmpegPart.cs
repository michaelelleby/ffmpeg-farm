using System;
using System.Diagnostics.Contracts;

namespace Contract
{
    public class FfmpegPart
    {
        public int Id { get; set; }
        public string SourceFilename { get; set; }
        public string Filename { get; set; }
        public int Number { get; set; }
        public int Target { get; set; }
        public bool IsAudio => Filename.EndsWith("_audio.mp4", StringComparison.InvariantCultureIgnoreCase);
        public Guid JobCorrelationId { get; set; }
        public double Psnr { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Bitrate { get; set; }
    }
}
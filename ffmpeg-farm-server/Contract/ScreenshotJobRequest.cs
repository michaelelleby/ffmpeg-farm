using System;

namespace Contract
{
    public class ScreenshotJobRequest : JobRequestBase
    {
        public string VideoSourceFilename { get; set; }

        public string DestinationFilename { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public TimeSpan ScreenshotTime { get; set; }

        public bool AspectRatio16_9 { get; set; }
    }
}
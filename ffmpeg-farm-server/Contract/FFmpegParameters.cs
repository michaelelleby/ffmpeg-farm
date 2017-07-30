using System.Drawing;

namespace Contract
{
    public class FFmpegParameters
    {
        public class Audio
        {
            public AudioCodec Codec { get; set; }

            public int Bitrate { get; set; }
        }

        public Audio AudioParam { get; set; }
        public string Inputfile { get; set; }
        public DeinterlaceSettings Deinterlace { get; set; }
        public Video VideoParam { get; set; }

        public class Video
        {
            public VideoCodec Codec { get; set; }
            public int Bitrate { get; set; }
            public VideoSize Size { get; set; }
            public string Preset { get; set; }
        }

        public class DeinterlaceSettings
        {
            public DeinterlaceMode Mode { get; set; }

            public enum DeinterlaceMode
            {
                Unknown = -1,
                SendFrame = 0,
                SendField = 1,
                SendFrameSkipSpatial = 2,
                SendFieldSkipSpatial = 3
            }

            public DeinterlaceParity Parity { get; set; }

            public enum DeinterlaceParity
            {
                Unknown = -2,
                Auto = -1,
                TopFieldFirst = 0,
                BottomFieldFirst = 1
            }

            public bool DeinterlaceAllFrames { get; set; }
        }
    }
}
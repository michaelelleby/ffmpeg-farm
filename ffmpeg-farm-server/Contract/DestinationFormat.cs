using System;
using Newtonsoft.Json;

namespace Contract
{
    public class VideoDestinationFormat
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public int VideoBitrate { get; set; }

        public int AudioBitrate { get; set; }

        public H264Profile Profile { get; set; }

        public string Level { get; set; }

        public string OutputExtension { get; set; }


        [JsonIgnore]
        public int Target { get; set; }
    }
}
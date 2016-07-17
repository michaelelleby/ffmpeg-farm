namespace Contract
{
    public class Quality
    {
        public int VideoBitrate { get; set; }

        public int AudioBitrate { get; set; }

        public H264Profile Profile { get; set; }

        public string Level { get; set; }
        public int Target { get; set; }
    }
}
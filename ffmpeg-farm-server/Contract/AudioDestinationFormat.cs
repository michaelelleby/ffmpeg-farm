namespace Contract
{
    public class AudioDestinationFormat
    {
        public AudioCodec AudioCodec { get; set; }

        public ContainerFormat Format { get; set; }
        
        public int Bitrate { get; set; }

        public Channels Channels { get; set; }
    }
}
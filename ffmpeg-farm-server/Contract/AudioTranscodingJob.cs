namespace Contract
{
    public class AudioTranscodingJob : FFmpegJob
    {
        public override JobType Type => JobType.Audio;
        public int Bitrate { get; set; }
        public int DestinationDurationSeconds { get; set; }
    }
}
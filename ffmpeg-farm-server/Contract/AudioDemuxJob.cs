namespace Contract
{
    public class AudioDemuxJob : FFmpegJob
    {
        public override JobType Type => JobType.AudioDemux;
    }
}
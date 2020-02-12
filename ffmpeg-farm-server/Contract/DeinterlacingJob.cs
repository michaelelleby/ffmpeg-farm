namespace Contract
{
    public class DeinterlacingJob : FFmpegJob
    {
        public override JobType Type => JobType.Deinterlacing;
    }
}
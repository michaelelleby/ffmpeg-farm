namespace Contract
{
    public class ScreenshotJob : FFmpegJob
    {
        public override JobType Type => JobType.Screenshot;
    }
}
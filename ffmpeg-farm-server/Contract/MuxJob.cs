namespace Contract
{
    public class MuxJob : FFmpegJob
    {
        public override JobType Type => JobType.Mux;
    }
}
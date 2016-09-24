namespace Contract
{
    public class MergeJob : VideoTranscodingJob
    {
        public new JobType Type => JobType.VideoMerge;
    }
}
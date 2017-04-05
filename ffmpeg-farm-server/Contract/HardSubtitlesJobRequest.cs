namespace Contract
{
    public class HardSubtitlesJobRequest : JobRequestBase
    {
        public string VideoSourceFilename { get; set; }

        public string SubtitlesFilename { get; set; }
    }
}
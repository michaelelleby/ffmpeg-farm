namespace Contract
{
    public class MuxJobRequest : JobRequestBase
    {
        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }
    }
}
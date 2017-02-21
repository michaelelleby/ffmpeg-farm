
namespace Contract
{
    public class HardSubtilesJobRequest : JobRequestBase
    {
        public string VideoSourceFilename { get; set; }

        public string SubtilesFilename { get; set; }
    }
}
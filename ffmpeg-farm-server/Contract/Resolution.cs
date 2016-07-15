using System.Collections.Generic;

namespace Contract
{
    public class Resolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public IEnumerable<Quality> Bitrates { get; set; }
    }
}
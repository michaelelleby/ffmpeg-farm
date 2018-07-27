using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public class LoudnessJobRequest : JobRequestBase
    {
        public List<string> SourceFilenames { get; set; }
    }
}

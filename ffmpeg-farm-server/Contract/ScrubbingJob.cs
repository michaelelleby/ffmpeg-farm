using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public class ScrubbingJob : FFmpegJob
    {
        public override JobType Type => JobType.Scrubbing;
    }
}

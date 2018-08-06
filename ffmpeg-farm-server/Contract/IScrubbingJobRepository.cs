using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface IScrubbingJobRepository : IJobRepository
    {
        Guid Add(ScrubbingJobRequest request, ICollection<ScrubbingJob> jobs);
    }
}

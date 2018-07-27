using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public interface ILoudnessJobRepository : IJobRepository
    {
        Guid Add(LoudnessJobRequest request, ICollection<LoudnessJob> jobs);
    }
}

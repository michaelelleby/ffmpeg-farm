using System.Collections.Generic;

namespace Contract
{
    public class JobResult
    {
        public IEnumerable<JobResultModel> Requests { get; set; }
    }
}
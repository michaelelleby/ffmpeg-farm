using System.Collections.Generic;
using Contract.Models;

namespace Contract
{
    public class JobStatusModel
    {
        public IEnumerable<JobRequestModel> Requests { get; set; }
    }
}
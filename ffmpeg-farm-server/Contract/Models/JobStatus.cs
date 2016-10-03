using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract.Models
{
    public class JobStatus
    {
        public Guid JobCorrelationId { get; set; }
        public TranscodingJobState State { get; set; }
        public DateTimeOffset Created { get; set; }
        public List<string> OutputFiles { get; set; }
    }
}

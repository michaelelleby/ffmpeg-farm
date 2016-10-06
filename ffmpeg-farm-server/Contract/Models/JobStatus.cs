using System;
using System.Collections.Generic;

namespace Contract.Models
{
    public class JobStatus
    {
        public Guid JobCorrelationId { get; set; }
        public TranscodingJobState State { get; set; }
        public DateTimeOffset Created { get; set; }
        public ICollection<string> OutputFiles { get; set; }
    }
}

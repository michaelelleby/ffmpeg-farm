using System;
using System.Collections.Generic;
using System.Linq;

namespace Contract
{
    public class JobResultModel
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public string SourceFilename { get; set; }

        public string DestinationFilename { get; set; }

        public DateTimeOffset Needed { get; set; }

        public DateTimeOffset Created { get; set; }

        public double Progress
        {
            get { return Jobs.Sum(x => Convert.ToInt32(x.Progress/x.ChunkDuration*100)) / Jobs.Count(); }
        }

        public IEnumerable<dynamic> Jobs { get; set; }
    }
}
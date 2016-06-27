using System;
using System.Collections.Generic;

namespace Contract.Models
{
    public class JobRequestModel
    {
        public Guid JobCorrelationId { get; set; }
        public bool MpegDash { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Needed { get; set; }
        public string DestinationFilename { get; set; }
        public string VideoSourceFilename { get; set; }
        public string AudioSourceFilename { get; set; }
        public IEnumerable<TranscodingJobModel> Jobs { get; set; }
    }
}
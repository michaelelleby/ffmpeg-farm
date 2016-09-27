using System;
using System.Collections.Generic;
using System.Linq;

namespace Contract.Models
{
    public class JobRequestModel
    {
        public Guid JobCorrelationId { get; set; }
        public TranscodingJobState State
        {
            get
            {
                if (Jobs.All(x => x.State == TranscodingJobState.Done))
                    return TranscodingJobState.Done;

                if (Jobs.All(j => j.State == TranscodingJobState.Queued))
                    return TranscodingJobState.Queued;

                if (Jobs.Any(j => j.State == TranscodingJobState.Failed))
                    return TranscodingJobState.Failed;

                if (Jobs.All(j => j.State == TranscodingJobState.Paused))
                    return TranscodingJobState.Paused;

                if (Jobs.Any(j => j.State == TranscodingJobState.InProgress))
                    return TranscodingJobState.InProgress;

                return TranscodingJobState.Unknown;
            }
        }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Needed { get; set; }

        public string DestinationFilenamePrefix { get; set; }

        public string SourceFilename { get; set; }

        public IEnumerable<TranscodingJobModel> Jobs { get; set; }
    }
}
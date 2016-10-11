using System;
using System.Collections.Generic;
using System.Linq;

namespace Contract.Models
{
    public class FfmpegJobModel
    {
        public FfmpegJobModel()
        {
            Tasks = new List<FfmpegTaskModel>();
        }

        public Guid JobCorrelationId { get; set; }

        public TranscodingJobState State
        {
            get
            {
                if (Tasks.All(x => x.State == TranscodingJobState.Done))
                    return TranscodingJobState.Done;

                if (Tasks.All(j => j.State == TranscodingJobState.Queued))
                    return TranscodingJobState.Queued;

                if (Tasks.Any(j => j.State == TranscodingJobState.Failed))
                    return TranscodingJobState.Failed;

                if (Tasks.All(j => j.State == TranscodingJobState.Paused))
                    return TranscodingJobState.Paused;

                if (Tasks.Any(j => j.State == TranscodingJobState.InProgress))
                    return TranscodingJobState.InProgress;

                return TranscodingJobState.Unknown;
            }
        }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Needed { get; set; }

        public IEnumerable<FfmpegTaskModel> Tasks { get; set; }
    }
}
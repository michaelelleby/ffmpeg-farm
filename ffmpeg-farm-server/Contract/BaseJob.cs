using System;
using API.Database;

namespace Contract
{
    public abstract class BaseJob
    {
        /// <summary>
        /// Job id
        /// </summary>
        public Guid JobCorrelationId { get; set; }

        public TranscodingJobState State { get; set; }

        /// <summary>
        /// Source filename
        /// </summary>
        public string SourceFilename { get; set; }
    }
}
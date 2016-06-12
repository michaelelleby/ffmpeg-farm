using System;

namespace Contract
{
    public class BaseJob
    {
        /// <summary>
        /// Job id
        /// </summary>
        public Guid JobCorrelationId { get; set; }

        /// <summary>
        /// Client machine name. Used when reporting progress updates
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Progress of how many seconds has been encoded
        /// Used when reporting progress updates
        /// </summary>
        public TimeSpan Progress { get; set; }

        /// <summary>
        /// Whether this job is done
        /// </summary>
        public bool Done { get; set; }

        public int Id { get; set; }
    }
}
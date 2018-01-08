using System;

namespace Contract
{
    public class BaseJob
    {
        /// <summary>
        /// Commandline arguments sent to FFmpeg
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Job id
        /// </summary>
        public Guid JobCorrelationId { get; set; }

        /// <summary>
        /// Client machine name. Used when reporting progress updates
        /// </summary>
        public string MachineName { get; set; }

        public DateTimeOffset Heartbeat { get; set; }

        public string HeartBeatMachineName { get; set; }

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
        public bool Failed { get; set; }
        public TranscodingJobState State { get; set; }

        /// <summary>
        /// Source filename
        /// </summary>
        public string SourceFilename { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public virtual JobType Type { get;} 
    }
}
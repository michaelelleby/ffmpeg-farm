namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegTasks
    {
        public int id { get; set; }

        public int FfmpegJobs_id { get; set; }

        [Required]
        public string Arguments { get; set; }

        public TranscodingJobState TaskState { get; set; }

        public int DestinationDurationSeconds { get; set; }

        public DateTimeOffset? Started { get; set; }

        public DateTimeOffset? Heartbeat { get; set; }

        [StringLength(50)]
        public string HeartbeatMachineName { get; set; }

        public double? Progress { get; set; }

        public double? VerifyProgress { get; set; }

        public string DestinationFilename { get; set; }

        public bool VerifyOutput { get; set; }

        public virtual FfmpegJobs Job { get; set; }
    }
}

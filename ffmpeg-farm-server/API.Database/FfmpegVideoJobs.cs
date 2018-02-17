namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegVideoJobs
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public double Progress { get; set; }

        public DateTime? Heartbeat { get; set; }

        [Required]
        public string Arguments { get; set; }

        public DateTime? Needed { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        public double ChunkDuration { get; set; }

        public string HeartbeatMachineName { get; set; }

        [Required]
        [StringLength(50)]
        public string State { get; set; }

        public DateTime? Started { get; set; }
    }
}

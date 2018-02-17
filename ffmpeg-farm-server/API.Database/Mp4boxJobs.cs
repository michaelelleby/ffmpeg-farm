namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Mp4boxJobs
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public DateTime? Heartbeat { get; set; }

        [Required]
        public string Arguments { get; set; }

        public DateTime? Needed { get; set; }

        public string HeartbeatMachineName { get; set; }

        [Required]
        [StringLength(50)]
        public string State { get; set; }
    }
}

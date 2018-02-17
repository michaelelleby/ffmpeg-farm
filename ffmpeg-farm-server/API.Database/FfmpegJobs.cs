namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegJobs
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public FfmpegJobs()
        {
            FfmpegTasks = new HashSet<FfmpegTasks>();
        }

        public int id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Needed { get; set; }

        public byte JobType { get; set; }

        public byte JobState { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<FfmpegTasks> FfmpegTasks { get; set; }
    }
}

namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("FfmpegAudioRequest")]
    public partial class FfmpegAudioRequest
    {
        public int id { get; set; }

        public Guid JobCorrelationId { get; set; }

        [Required]
        public string SourceFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }

        public DateTimeOffset? Needed { get; set; }

        public DateTimeOffset Created { get; set; }

        [Required]
        public string OutputFolder { get; set; }
    }
}

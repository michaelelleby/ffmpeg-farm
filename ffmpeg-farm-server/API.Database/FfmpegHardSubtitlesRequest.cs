namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("FfmpegHardSubtitlesRequest")]
    public partial class FfmpegHardSubtitlesRequest
    {
        [Key]
        public Guid JobCorrelationId { get; set; }

        [Required]
        public string VideoSourceFilename { get; set; }

        [Required]
        public string SubtitlesFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        public DateTimeOffset Needed { get; set; }

        public DateTimeOffset Created { get; set; }
    }
}

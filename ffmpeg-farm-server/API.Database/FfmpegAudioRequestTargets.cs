namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegAudioRequestTargets
    {
        public int id { get; set; }

        public Guid JobCorrelationId { get; set; }

        [Required]
        [StringLength(50)]
        public string Codec { get; set; }

        [Required]
        [StringLength(50)]
        public string Format { get; set; }

        public int Bitrate { get; set; }
    }
}

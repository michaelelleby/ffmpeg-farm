namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegVideoRequestTargets
    {
        public int id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int VideoBitrate { get; set; }

        public int AudioBitrate { get; set; }

        [Required]
        [StringLength(255)]
        public string H264Profile { get; set; }

        [Required]
        [StringLength(3)]
        public string H264Level { get; set; }
    }
}

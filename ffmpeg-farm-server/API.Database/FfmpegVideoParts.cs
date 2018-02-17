namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class FfmpegVideoParts
    {
        public int id { get; set; }

        public Guid JobCorrelationId { get; set; }

        [Required]
        public string Filename { get; set; }

        public int Number { get; set; }

        public int Target { get; set; }

        public int FfmpegJobs_Id { get; set; }

        public double PSNR { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Bitrate { get; set; }
    }
}

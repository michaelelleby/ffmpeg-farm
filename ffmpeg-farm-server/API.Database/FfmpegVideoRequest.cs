namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("FfmpegVideoRequest")]
    public partial class FfmpegVideoRequest
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }

        public DateTime Needed { get; set; }

        public DateTime? Created { get; set; }

        public bool EnableDash { get; set; }

        public bool EnableTwoPass { get; set; }

        public bool EnablePsnr { get; set; }

        [StringLength(50)]
        public string Inpoint { get; set; }
    }
}

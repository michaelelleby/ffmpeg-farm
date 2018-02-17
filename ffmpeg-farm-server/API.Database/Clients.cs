namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Clients
    {
        public int id { get; set; }

        [Required]
        [StringLength(50)]
        public string MachineName { get; set; }

        public DateTimeOffset LastHeartbeat { get; set; }
    }
}

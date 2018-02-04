namespace API.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Log")]
    public partial class Log
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Application { get; set; }

        public DateTime Logged { get; set; }

        [Required]
        [StringLength(50)]
        public string Level { get; set; }

        [Required]
        public string Message { get; set; }

        [StringLength(250)]
        public string UserName { get; set; }

        public string ServerName { get; set; }

        public string Port { get; set; }

        public string Url { get; set; }

        public bool? Https { get; set; }

        [StringLength(100)]
        public string ServerAddress { get; set; }

        [StringLength(100)]
        public string RemoteAddress { get; set; }

        [StringLength(250)]
        public string Logger { get; set; }

        public string Callsite { get; set; }

        public string Exception { get; set; }
    }
}

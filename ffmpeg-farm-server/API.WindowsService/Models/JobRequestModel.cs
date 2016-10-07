using System;
using System.ComponentModel.DataAnnotations;

namespace API.WindowsService.Models
{
    public class JobRequestModel
    {
        [Required]
        public string OutputFolder { get; set; }

        [Required]
        public DateTimeOffset Needed { get; set; }

        public TimeSpan Inpoint { get; set; }
    }
}

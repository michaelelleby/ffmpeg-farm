using System;
using System.ComponentModel.DataAnnotations;
using Contract;

namespace API.WindowsService.Models
{
    public class JobRequestModel
    {
        [Required]
        public string SourceFilename { get; set; }

        [Required]
        public string OutputFolder { get; set; }

        [Required]
        public DateTimeOffset Needed { get; set; }

        public string DestinationFilenamePrefix { get; set; }

        public TimeSpan Inpoint { get; set; }
    }
}

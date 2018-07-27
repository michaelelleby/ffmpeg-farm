using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.WindowsService.Models
{
    public class LoudnessJobRequestModel : JobRequestModel
    {
        [Required]
        public string AudioPresetFile { get; set; }
        [Required]
        public string DestinationFilename { get; set; }
        [Required]
        public List<string> SourceFilenames { get; set; }
    }
}

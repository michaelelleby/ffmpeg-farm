using System.ComponentModel.DataAnnotations;

namespace API.WindowsService.Models
{
    public class DeinterlacingJobRequestModel : JobRequestModel
    {
        [Required]
        public string VideoSourceFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }
        
    }
}
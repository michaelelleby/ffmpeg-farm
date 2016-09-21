using System.ComponentModel.DataAnnotations;
using Contract;

namespace API.WindowsService.Models
{
    public class AudioJobRequestModel : JobRequestModel
    {
        [Required]
        public AudioDestinationFormat[] Targets { get; set; }
    }
}
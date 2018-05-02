using System.ComponentModel.DataAnnotations;
using API.WindowsService.Validators;
using FluentValidation.Attributes;

namespace API.WindowsService.Models
{
    [Validator(typeof(HardSubtitlesJobRequestValidator))]
    public class HardSubtitlesJobRequestModel : JobRequestModel
    {
        [Required]
        public string VideoSourceFilename { get; set; }

        [Required]
        public string SubtitlesFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }

        public string CodecId { get; set; }
    }
}

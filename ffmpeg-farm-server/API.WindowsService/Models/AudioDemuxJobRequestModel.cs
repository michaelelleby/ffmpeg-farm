using System.ComponentModel.DataAnnotations;
using API.WindowsService.Validators;
using FluentValidation.Attributes;

namespace API.WindowsService.Models
{
    [Validator(typeof(AudioDemuxJobRequestValidator))]
    public class AudioDemuxJobRequestModel : JobRequestModel
    {
        [Required]
        public string VideoSourceFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }
    }
}
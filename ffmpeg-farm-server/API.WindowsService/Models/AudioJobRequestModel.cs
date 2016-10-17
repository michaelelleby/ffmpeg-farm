using System.ComponentModel.DataAnnotations;
using API.WindowsService.Validators;
using Contract;
using FluentValidation.Attributes;

namespace API.WindowsService.Models
{
    [Validator(typeof(AudioRequestValidator))]
    public class AudioJobRequestModel : JobRequestModel
    {
        [Required]
        public AudioDestinationFormat[] Targets { get; set; }

        [Required]
        public string DestinationFilenamePrefix { get; set; }

        [Required]
        public string SourceFilename { get; set; }
    }
}
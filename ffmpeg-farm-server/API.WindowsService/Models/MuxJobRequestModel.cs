using System.ComponentModel.DataAnnotations;
using API.WindowsService.Validators;
using FluentValidation.Attributes;

namespace API.WindowsService.Models
{
    [Validator(typeof(MuxJobRequestValidator))]
    public class MuxJobRequestModel : JobRequestModel
    {
        [Required]
        public string VideoSourceFilename { get; set; }

        [Required]
        public string AudioSourceFilename { get; set; }

        [Required]
        public string DestinationFilename { get; set; }
    }
}
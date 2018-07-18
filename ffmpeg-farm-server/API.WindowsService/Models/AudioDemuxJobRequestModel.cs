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

        /// <summary>
        /// Indicates witch stream from the input file to use as left stereo channel. Default is 1.
        /// If left and right streams are the same we assume that stream number is stereo already.
        /// </summary>
        public int LeftStream { get; set; } = 1;

        /// <summary>
        /// Indicates witch stream from the input file to use as right stereo channel. Default is 1.
        /// If left and right streams are the same we assume that stream number is stereo already.
        /// </summary>
        public int RightStream { get; set; } = 1;
    }
}
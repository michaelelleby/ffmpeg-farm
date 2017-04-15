using System.ComponentModel.DataAnnotations;
using Contract;
using API.WindowsService.Validators;
using FluentValidation.Attributes;

namespace API.WindowsService.Models
{
    [Validator(typeof(VideoRequestValidator))]
    public class VideoJobRequestModel : JobRequestModel
    {
        [Required]
        public string FFmpegPreset { get; set; }

        [Required]
        public ContainerFormat ContainerFormat { get; set; }

        [Required]
        public VideoDestinationFormat[] Targets { get; set; }

        public string AudioSourceFilename { get; set; }

        public bool EnableMpegDash { get; set; }

        public bool EnableTwoPass { get; set; }

        public bool EnablePsnr { get; set; }
        public string DestinationFilenamePrefix { get; set; }

        [Required]
        public string SourceFilename { get; set; }
    }
}

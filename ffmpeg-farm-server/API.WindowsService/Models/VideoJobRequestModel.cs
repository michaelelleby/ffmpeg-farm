using System.ComponentModel.DataAnnotations;
using Contract;

namespace API.WindowsService.Models
{
    public class VideoJobRequestModel : JobRequestModel
    {
        [Required]
        public string FFmpegPreset { get; set; }

        [Required]
        public ContainerFormat ContainerFormat { get; set; }

        [Required]
        public DestinationFormat[] Targets { get; set; }

        public string AudioSourceFilename { get; set; }

        public bool EnableMpegDash { get; set; }

        public bool EnableTwoPass { get; set; }

        public bool EnablePsnr { get; set; }
    }
}

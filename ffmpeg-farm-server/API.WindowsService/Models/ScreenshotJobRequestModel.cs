using System;
using System.ComponentModel.DataAnnotations;

namespace API.WindowsService.Models
{
    public class ScreenshotJobRequestModel : JobRequestModel
    {
        [Required]
        public string VideoSourceFilename { get; set; }
        
        [Required]
        public string DestinationFilename { get; set; }

        [Required]
        public int Width { get; set; }

        [Required]
        public int Height { get; set; }

        [Required]
        public TimeSpan ScreenshotTime { get; set; }

        [Required]
        public bool AspectRatio16_9 { get; set; }
    }
}
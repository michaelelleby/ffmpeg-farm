using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract;

namespace API.WindowsService.Models
{
    public class ScrubbingJobRequestModel : JobRequestModel
    {
        [Required]
        public string SourceFilename { get; set; }

        [Required]
        public int FirstThumbnailOffsetInSeconds { get; set; }
        [Required]
        public int MaxSecondsBetweenThumbnails { get; set; }
        
        /// <summary>
        /// Each resolution must be in the format w:h example 160:80
        /// </summary>
        [Required]
        public List<string> ThumbnailResoultions { get; set; }
        
        /// <summary>
        /// Allowed values are "FiveByFive" and "TenByTen".
        /// </summary>
        [Required]
        public List<string> SpriteSheetSizes { get; set; }
    }
}

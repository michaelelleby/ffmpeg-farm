using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract
{
    public class ScrubbingJobRequest : JobRequestBase
    {
        public string SourceFilename { get; set; }
        public int FirstThumbnailOffsetInSeconds { get; set; }
        public int MaxSecondsBetweenThumbnails { get; set; }
        /// <summary>
        /// Each resolution must be in the format w:h example 160:80
        /// </summary>
        public List<string> ThumbnailResolutions { get; set; }
        public List<SpriteSheetSize> SpriteSheetSizes { get; set; }
    }
}

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
        public List<SpriteSheetSize> SpriteSheetSizes {
            get
            {
                if (this.spriteSheetSizes == null)
                    this.spriteSheetSizes = new List<SpriteSheetSize>();
                return this.spriteSheetSizes;
            }
            set { this.spriteSheetSizes = value; }
        }
        private List<SpriteSheetSize> spriteSheetSizes;


        public override bool Equals(object other)
        {
            var b = other as ScrubbingJobRequest;
            if (b == null)
                return false;

            var areSpriteSheetSizesEqual = true;
            for (var i = 0; i < SpriteSheetSizes.Count; i++)
            {
                if (SpriteSheetSizes[i] != b.SpriteSheetSizes[i])
                    areSpriteSheetSizesEqual = false;
            }

            return (Needed == b.Needed &&
                    DestinationFilename == b.DestinationFilename &&
                    OutputFolder == b.OutputFolder &&
                    SourceFilename == b.SourceFilename &&
                    FirstThumbnailOffsetInSeconds == b.FirstThumbnailOffsetInSeconds &&
                    MaxSecondsBetweenThumbnails == b.MaxSecondsBetweenThumbnails &&
                    ThumbnailResolutions == b.ThumbnailResolutions &&
                    areSpriteSheetSizesEqual);
        }
    }
}

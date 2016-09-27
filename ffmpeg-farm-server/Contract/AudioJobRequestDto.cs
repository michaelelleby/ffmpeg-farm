using System;
using System.Collections.Generic;
using System.Linq;

namespace Contract
{
    public class AudioJobRequestDto
    {
        public AudioJobRequestDto()
        {
            Jobs = new List<AudioTranscodingJobDto>();
        }

        public Guid JobCorrelationId { get; set; }

        public string SourceFilename { get; set; }
        
        public string DestinationFilename { get; set; }

        public DateTimeOffset? Needed { get; set; }

        public DateTimeOffset Created { get; set; }

        public string OutputFolder { get; set; }

        public ICollection<AudioTranscodingJobDto> Jobs { get; set; }
    }
}
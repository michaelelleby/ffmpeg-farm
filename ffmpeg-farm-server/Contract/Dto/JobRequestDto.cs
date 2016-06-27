using System;

namespace Contract.Dto
{
    public class JobRequestDto
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public string VideoSourceFilename { get; set; }

        public string AudioSourceFilename { get; set; }

        public string DestinationFilename { get; set; }

        public DateTimeOffset Needed { get; set; }

        public DateTimeOffset Created { get; set; }

        public bool EnableDash { get; set; }
    }
}
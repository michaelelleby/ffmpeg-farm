using System;
using System.Collections.Generic;
using API.Database;

namespace Contract.Dto
{
    public class FFmpegJobDto
    {
        public int Id { get; set; }

        public Guid JobCorrelationId { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset Needed { get; set; }

        public TranscodingJobState State { get; set; }

        public ICollection<FFmpegTaskDto> Tasks { get; set; }

        public JobType Type { get; set; }
    }
}
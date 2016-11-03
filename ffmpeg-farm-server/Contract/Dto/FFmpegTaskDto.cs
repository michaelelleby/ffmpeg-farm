using System;

namespace Contract.Dto
{
    public class FFmpegTaskDto
    {
        public int Id { get; set; }

        public int FfmpegJobsId { get; set; }

        public string Arguments { get; set; }

        public TranscodingJobState State { get; set; }

        public DateTimeOffset? Started { get; set; }

        public DateTimeOffset? Heartbeat { get; set; }

        public string HeartbeatMachineName { get; set; }

        public double Progress { get; set; }

        public int DestinationDurationSeconds { get; set; }

        public string DestinationFilename { get; set; }

        public bool VerifyOutput { get; set; }
    }
}
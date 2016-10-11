using System;
using Contract;

namespace API.WindowsService.Models
{
    public class FfmpegTaskModel
    {
        public DateTimeOffset Heartbeat { get; set; }
        public string HeartbeatMachine { get; set; }
        public TranscodingJobState State { get; set; }
    }
}
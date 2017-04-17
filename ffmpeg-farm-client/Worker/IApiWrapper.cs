using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFmpegFarm.Worker.Client;

namespace FFmpegFarm.Worker
{
    public interface IApiWrapper
    {
        int? ThreadId { get; set; } // main thread id, used for logging in child threads.
        FFmpegTaskDto GetNext(string machineName);
        bool UpdateProgress(TaskProgressModel model, bool ignoreCancel = false);
    }
}

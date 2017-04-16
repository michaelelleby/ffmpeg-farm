using System;
using FFmpegFarm.Worker.Client;

namespace FFmpegFarm.Worker
{
    public interface IProgressUpdater : IDisposable
    {
        void UpdateTask(FFmpegTaskDto task);
    }
}
using System;
using FFmpegFarm.Worker.Client;

namespace FFmpegFarm.Worker.ProgressUpdaters
{
    public class HttpProgressUpdater : IProgressUpdater
    {
        private readonly IApiWrapper _apiWrapper;

        public HttpProgressUpdater(IApiWrapper apiWrapper)
        {
            if (apiWrapper == null) throw new ArgumentNullException(nameof(apiWrapper));

            _apiWrapper = apiWrapper;
        }

        public void UpdateTask(FFmpegTaskDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            try
            {
                var model = new TaskProgressModel
                {
                    Done = dto.State == FFmpegTaskDtoState.Done,
                    Failed = dto.State == FFmpegTaskDtoState.Failed,
                    Id = dto.Id.GetValueOrDefault(0),
                    MachineName = dto.HeartbeatMachineName,
                    Progress = TimeSpan.FromSeconds(dto.Progress.GetValueOrDefault(0)).ToString("c"),
                    VerifyProgress =dto.VerifyProgress.HasValue ? TimeSpan.FromSeconds(dto.VerifyProgress.Value).ToString("c") : null,
                    Timestamp = DateTimeOffset.Now
                };

                Response state = _apiWrapper.UpdateProgress(model);

                if (state == Response.Canceled)
                {
                    //KillProcess("Canceled from ffmpeg server");
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Dispose()
        {
        }
    }
}
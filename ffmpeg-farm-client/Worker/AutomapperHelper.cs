using FFmpegFarm.Worker.Client;

namespace FFmpegFarm.Worker
{
    public static class AutomapperHelper
    {
        static AutomapperHelper()
        {
            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<AudioTranscodingJob, BaseJob>();
            });
            AutoMapper.Mapper.Configuration.AssertConfigurationIsValid();
        }

        public static BaseJob ToBaseJob(this AudioTranscodingJob audioTranscodingJob)
        {
            return AutoMapper.Mapper.Map<BaseJob>(audioTranscodingJob);
        }
    }
}

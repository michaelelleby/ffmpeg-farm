using System;
using System.Configuration;
using API.Logging;
using API.Repository;
using API.Service;
using Contract;
using StructureMap;

namespace API.IoC
{
    public class IoC
    {
        public static void ConfigureContainer(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            container.Configure(_ =>
            {
                _.For<IVideoJobRepository>()
                    .Use<VideoJobRepository>();

                _.For<IAudioJobRepository>()
                    .Use<AudioJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IMuxJobRepository>()
                    .Use<MuxJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IHardSubtitlesJobRepository>()
                    .Use<HardSubtitlesJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IJobRepository>()
                    .Use<JobRepository>();

                _.For<IHelper>()
                    .Use<Helper>();

                _.For<ILogging>()
                    .Singleton()
                    .Use<NLogWrapper>()
                    .Ctor<string>("name")
                    .Is(ConfigurationManager.AppSettings["NLog-Appname"] ??
                        System.Reflection.Assembly.GetExecutingAssembly().FullName);
            });
        }
    }
}
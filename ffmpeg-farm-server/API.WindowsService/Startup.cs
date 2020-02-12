using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using API.Logging;
using API.Repository;
using API.Service;
using API.WindowsService.Filters;
using Contract;
using DR.Common.Monitoring.Contract;
using FluentValidation.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Owin;
using StructureMap;
using Swashbuckle.Application;

namespace API.WindowsService
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            // only allow json...
            config.Formatters.Clear();

            config.Formatters.Add(new JsonMediaTypeFormatter
            {
                Indent = true,
                SerializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new StringEnumConverter() }
                }
            });

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            // Get documentation
            var xmlDocFiles =
                Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\App_Data\\")
                .Where(filename => filename.EndsWith(".XML", StringComparison.InvariantCultureIgnoreCase));

            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "FFmpeg Farm controller API");
                c.DescribeAllEnumsAsStrings();
                foreach (var xmlDocFile in xmlDocFiles) c.IncludeXmlComments(xmlDocFile);
            }).EnableSwaggerUi(c =>
            {
                c.DisableValidator();
            });
            config.Routes.MapHttpRoute(
                name: "custom_swagger_ui_shortcut",
                routeTemplate: "",
                defaults: null,
                constraints: null,
                handler: new RedirectHandler(SwaggerDocsConfig.DefaultRootUrlResolver, "/swagger"));
            config.MapHttpAttributeRoutes();

            var container = new Container();
            container.Configure(_ =>
            {
                _.For<IScrubbingJobRepository>()
                    .Use<ScrubbingJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<ILoudnessJobRepository>()
                    .Use<LoudnessJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IAudioJobRepository>()
                    .Use<AudioJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IAudioDemuxJobRepository>()
                    .Use<AudioDemuxJobRepository>()
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

                _.For<IScreenshotJobRepository>()
                    .Use<ScreenshotJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IDeinterlacingJobRepository>()
                    .Use<DeinterlacingJobJobRepository>()
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

                _.For<IHealthCheck>().Add<DatabaseHealthCheck>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IHealthCheck>().Add<FileShareHealthCheck>()
                    .Ctor<IEnumerable<string>>("files")
                    .Is(new []
                    {
                        ConfigurationManager.AppSettings["MediaInfoPath"],
                        ConfigurationManager.AppSettings["FFmpeg_3_2"],
                        ConfigurationManager.AppSettings["FFmpeg_3_4_1"],

                    })
                    .Ctor<IEnumerable<string>>("folders")
                    .Is(new []
                    {
                        ConfigurationManager.AppSettings["FFmpegLogPath"],
                    });

                if (!int.TryParse(ConfigurationManager.AppSettings["WorkerNodesHealthCheckWindowInMinutes"],
                    out var windowInMinutes))
                {
                    windowInMinutes = 60;
                }

                if (!int.TryParse(ConfigurationManager.AppSettings["WorkerNodesHealthCheckMinimumErrors"],
                    out var minimumErrors))
                {
                    minimumErrors = 3;
                }

                if (!float.TryParse(ConfigurationManager.AppSettings["WorkerNodesHealthCheckMinimumErrorRate"],
                    out var minimumErrorRate))
                {
                    minimumErrorRate = 0.25f;
                }

                _.For<IHealthCheck>().Add<WorkerNodesHealthCheck>()
                    .Ctor<int>("windowInMinutes").Is(windowInMinutes)
                    .Ctor<int>("minimumErrors").Is(minimumErrors)
                    .Ctor<float>("minimumErrorRate").Is(minimumErrorRate);

                _.For<ISystemStatus>().Add<DR.Common.Monitoring.SystemStatus>()
                    .Ctor<bool>("isPrivileged").Is(true);
            });

            config.DependencyResolver = new StructureMapDependencyResolver(container);

            config.Filters.Add(new ExceptionFilter((ILogging) config.DependencyResolver.GetService(typeof(ILogging))));

            FluentValidationModelValidatorProvider.Configure(config);

            appBuilder.UseWebApi(config);
        }
    }
}
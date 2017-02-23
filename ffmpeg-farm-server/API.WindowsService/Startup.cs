using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using API.Repository;
using API.Service;
using API.WindowsService.Filters;
using Contract;
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
            });

            config.DependencyResolver = new StructureMapDependencyResolver(container);

            config.Filters.Add(new ExceptionFilter());

            FluentValidationModelValidatorProvider.Configure(config);

            appBuilder.UseWebApi(config);
        }
    }
}
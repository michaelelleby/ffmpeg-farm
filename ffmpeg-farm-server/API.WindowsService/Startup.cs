using System.Collections.Generic;
using System.Configuration;
using System.Web.Http;
using API.Repository;
using API.WindowsService.Filters;
using Contract;
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

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "A title for your API");
                c.DescribeAllEnumsAsStrings();
            }).EnableSwaggerUi();

            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            };

            var container = new Container();
            container.Configure(_ =>
            {
                _.For<IVideoJobRepository>()
                    .Use<VideoJobRepository>();

                _.For<IAudioJobRepository>()
                    .Use<AudioJobRepository>()
                    .Ctor<string>("connectionString")
                    .Is(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);

                _.For<IJobRepository>()
                    .Use<JobRepository>();
            });

            config.DependencyResolver = new StructureMapDependencyResolver(container);

            config.Filters.Add(new ExceptionFilter());

            appBuilder.UseWebApi(config);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using API.IoC;
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
            IEnumerable<string> xmlDocFiles =
                Directory.GetFiles($"{AppDomain.CurrentDomain.BaseDirectory}\\App_Data\\")
                    .Where(filename => filename.EndsWith(".XML", StringComparison.InvariantCultureIgnoreCase));

            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "FFmpeg Farm controller API");
                c.DescribeAllEnumsAsStrings();
                foreach (string xmlDocFile in xmlDocFiles)
                    c.IncludeXmlComments(xmlDocFile);
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

            config.DependencyResolver = new StructureMapDependencyResolver(new Container(new ApiRegistry()));

            config.Filters.Add(new ExceptionFilter((ILogging)config.DependencyResolver.GetService(typeof(ILogging))));

            FluentValidationModelValidatorProvider.Configure(config);

            appBuilder.UseWebApi(config);

            config.EnsureInitialized();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Http;
using API.WindowsService.Controllers;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Owin;
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

            if (bool.Parse(ConfigurationManager.AppSettings["CreateDatabaseOnStartup"]))
            {
                InitDatabase();
            }

            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "A title for your API");
                c.DescribeAllEnumsAsStrings();
                c.IncludeXmlComments(GetXmlCommentsPathForControllers());
                c.IncludeXmlComments(GetXmlCommentsPathForContract());
            }).EnableSwaggerUi();

            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new IsoDateTimeConverter(), 
                    new StringEnumConverter()
                },
                TypeNameHandling = TypeNameHandling.All
            };

            appBuilder.UseWebApi(config);
        }

        private string GetXmlCommentsPathForContract()
        {
            var uriString = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + Path.DirectorySeparatorChar + "App_Data" + Path.DirectorySeparatorChar + "API.Contract.xml";
            return new Uri(uriString).LocalPath;
        }

        private string GetXmlCommentsPathForControllers()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".XML";
            var commentsFile = Path.Combine(baseDirectory, commentsFileName);

            return commentsFile;
        }

        private static void InitDatabase()
        {
            using (var connection = Helper.GetConnection())
            {
                connection.Open();

                var uriString = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase) + Path.DirectorySeparatorChar + "App_Data" + Path.DirectorySeparatorChar + "create_database.sql";
                var path = new Uri(uriString).LocalPath;
                string script = File.ReadAllText(path);
                connection.Execute(script);
            }
        }
    }
}
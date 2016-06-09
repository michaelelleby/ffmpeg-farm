using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Http;
using Dapper;
using ffmpeg_farm_server.Controllers;

namespace ffmpeg_farm_server
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            using (var connection = Helper.GetConnection())
            {
                string path = HostingEnvironment.MapPath(@"/App_Data/create_database.sql");
                string script = File.ReadAllText(path);
                connection.Execute(script);
            }
        }
    }
}

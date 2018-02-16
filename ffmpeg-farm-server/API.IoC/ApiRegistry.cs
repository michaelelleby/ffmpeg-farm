using System.Configuration;
using API.Logging;
using API.Service;
using Contract;
using StructureMap;

namespace API.IoC
{
    public class ApiRegistry : Registry
    {
        public ApiRegistry()
        {
            For<IHelper>()
                .Use<Helper>();

            For<ILogging>()
                .Singleton()
                .Use<NLogWrapper>()
                .Ctor<string>("name")
                .Is(ConfigurationManager.AppSettings["NLog-Appname"] ??
                    System.Reflection.Assembly.GetExecutingAssembly().FullName);

            For<IApiSettings>()
                .Use<ApiSettings>();
        }
    }
}

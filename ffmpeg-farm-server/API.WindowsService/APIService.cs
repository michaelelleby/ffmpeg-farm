using System;
using System.Configuration;
using System.ServiceProcess;
using API.StatusPoller;
using Contract;
using Microsoft.Owin.Hosting;
using StructureMap;

namespace API.WindowsService
{
    public partial class APIService : ServiceBase
    {
        private IDisposable _server = null;
        private Poller _poller;

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            Stop();
            base.OnStop();
        }

        public void Start()
        {
            var port = 9000;
            var url = Environment.UserInteractive ? $"http://localhost:{port}/" : $"http://+:{port}/";
            var readableUrl = url.Replace("+", Environment.MachineName);
            _server = WebApp.Start<Startup>(url);

            if (RabbitMqEnabled())
            {
                IContainer container = new Container();
                IoC.IoC.ConfigureContainer(container);
                _poller = new Poller(container.GetInstance<IJobRepository>(), container.GetInstance<ILogging>(),
                    ConfigurationManager.AppSettings["RabbitMqDsn"],
                    ConfigurationManager.AppSettings["RabbitMqQueueName"]);

                _poller.Start();
            }
        }

        private static bool RabbitMqEnabled()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["RabbitMqEnabled"]))
                return false;

            return Convert.ToBoolean(ConfigurationManager.AppSettings["RabbitMqEnabled"]);
        }

        public void Stop()
        {
            _server?.Dispose();
            _poller?.Dispose();
        }
    }
}
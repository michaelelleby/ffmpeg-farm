using System;
using System.ServiceProcess;
using API.WindowsService;
using Microsoft.Owin.Hosting;

namespace API.WindowsService
{
    public class APIService : ServiceBase
    {
        private IDisposable _server = null;

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
        }

        public new void Stop()
        {
            _server?.Dispose();
        }
    }
}
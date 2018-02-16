using System;
using System.ServiceProcess;
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
            const int port = 9000;
            string url = Environment.UserInteractive ? $"http://localhost:{port}/" : $"http://+:{port}/";
            _server = WebApp.Start<Startup>(url);
        }

        public new void Stop()
        {
            _server?.Dispose();
        }
    }
}
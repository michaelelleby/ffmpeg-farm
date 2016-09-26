using System;
using System.ServiceProcess;
using API.WindowsService;
using Microsoft.Owin.Hosting;

namespace API.WindowsService
{
    public partial class APIService : ServiceBase
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
            var url = "http://+:9000/";
            _server = WebApp.Start<Startup>(url);
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
            }
        }
    }
}
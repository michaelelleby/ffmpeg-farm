using System;
using System.ServiceProcess;
using API.WindowsService;
using Microsoft.Owin.Hosting;

namespace API.WindowsService
{
    public partial class APIService : ServiceBase
    {
        public string baseAddress = "http://localhost:9000/";
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
            _server = WebApp.Start<Startup>(url: baseAddress);
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
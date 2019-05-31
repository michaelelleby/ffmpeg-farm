using System;
using System.ServiceProcess;
using System.Timers;
using API.Repository;
using API.Service;
using Microsoft.Owin.Hosting;

namespace API.WindowsService
{
    public class APIService : ServiceBase
    {
        private IDisposable _server = null;
        private Timer _timer;
        private Janitor _janitor;

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
            _janitor = new Janitor(new Helper());
            _timer = new Timer(TimeSpan.FromDays(1).TotalMilliseconds) { AutoReset = true, Enabled = true };
            _timer.Elapsed += (sender, args) =>
            {
                if (!System.Threading.Monitor.TryEnter(_janitor)) return;
                _janitor.CleanUp();
                System.Threading.Monitor.Exit(_janitor);
            };
        }

        public new void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _server?.Dispose();
        }
    }
}
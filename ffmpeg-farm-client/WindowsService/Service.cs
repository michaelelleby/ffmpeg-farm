using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using FFmpegFarm.WindowsService.Properties;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    public partial class Service : ServiceBase
    {
        private IList<Thread> _threads;
        private CancellationTokenSource _cancellationTokenSource;
        private static ILogger _logger = new DummyLogger();
        public Service()
        {
            InitializeComponent();
        }

        private class DummyLogger : ILogger
        {
            public void Debug(string text) { }

            public void Warn(string text) { }

            public void Exception(Exception exception) { }
        }

        protected override void OnStart(string[] args)
        {
            _threads = new List<Thread>();
            _cancellationTokenSource = new CancellationTokenSource();
            for (var x = 0; x < Settings.Default.Threads; x++)
            {
                var thread = new Thread(() => new Worker.Node(Settings.Default.FFmpegPath, Settings.Default.ControllerApi, _logger).Run(_cancellationTokenSource.Token));
                _threads.Add(thread);
                thread.Start();
            }
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }
    }
}

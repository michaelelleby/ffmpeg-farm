using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.WindowsService.Properties;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    public partial class Service : ServiceBase
    {
        private IList<Task> _tasks;
        private CancellationTokenSource _cancellationTokenSource;
        private static readonly ILogger _logger = new DummyLogger();
        public Service()
        {
            InitializeComponent();
        }

        private class DummyLogger : ILogger
        {
           
            public void Debug(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            {
                
            }

            public void Warn(string text, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            {
                
            }

            public void Exception(Exception exception, int? threadId = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            {
                
            }
        }

        protected override void OnStart(string[] args)
        {
            _tasks = new List<Task>();
            for (var x = 0; x < Settings.Default.Threads; x++)
            {
                var task =
                    Task.Factory.StartNew(
                        () =>
                        new Node(Settings.Default.FFmpegPath, Settings.Default.ControllerApi, _logger)
                        .Run(_cancellationTokenSource.Token));
                task.ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnCanceled);
                _tasks.Add(task);
            }
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            while (_tasks.Any(t => !t.IsCompleted))
            {
                Thread.Sleep(10);
            }
        }
    }
}

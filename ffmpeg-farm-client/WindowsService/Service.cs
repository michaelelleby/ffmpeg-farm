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
        private Task[] _tasks;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        public Service()
        {
            InitializeComponent();
            _logger = new EventLogger(eventLog);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Information($"Starting service\n{Settings.Default.FFmpegPath}\n{Settings.Default.ControllerApi}\n{Settings.Default.Threads} threads.");
            _tasks = new Task[Settings.Default.Threads];
            for (var x = 0; x < Settings.Default.Threads; x++)
            {

                var task = Node.GetNodeTask(
                    Settings.Default.FFmpegPath,
                    Settings.Default.ControllerApi,
                    _logger,
                    _cancellationTokenSource.Token);

                task.Start();
                _tasks[x] = task;
            }
        }

        protected override void OnStop()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                // ReSharper disable once MethodSupportsCancellation
                Task.WaitAll(_tasks);
            }
            catch (Exception e)
            {
                if (!(e.InnerException?.GetType() == typeof(OperationCanceledException)
                    || e.InnerException?.GetType() == typeof(TaskCanceledException)))
                    throw;
            }
            _logger.Information("Stopped");
        }
    }
}

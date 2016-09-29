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
            _tasks = new List<Task>();
            for (var x = 0; x < Settings.Default.Threads; x++)
            {

                var task = Node.GetNodeTask(
                    Settings.Default.FFmpegPath,
                    Settings.Default.ControllerApi,
                    _logger,
                    _cancellationTokenSource.Token);
                
                //task.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnCanceled);
                //task.ContinueWith(t => { _logger.Exception(t.Exception); }, TaskContinuationOptions.NotOnCanceled);
                task.Start();
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
            _logger.Information("Stopped");
        }
    }
}

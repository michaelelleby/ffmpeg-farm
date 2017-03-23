using System.ServiceProcess;
using System.Threading;
using FFmpegFarm.Worker;

namespace FFmpegFarm.WindowsService
{
    public partial class Service : ServiceBase
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private readonly FFmpegClient _client;

        public Service()
        {
            InitializeComponent();
            _logger = new EventLogger(eventLog);
            _cancellationTokenSource = new CancellationTokenSource();
            _client = new FFmpegClient(_logger, _cancellationTokenSource);
        }

        protected override void OnStart(string[] args)
        {
            _client.Start();
        }

        protected override void OnStop()
        {
            _client.Stop();
        }
    }
}

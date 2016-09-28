using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using FFmpegFarm.WindowsService.Properties;

namespace FFmpegFarm.WindowsService
{
    public partial class Service : ServiceBase
    {
        private IList<Thread> _threads;
        private CancellationTokenSource _cancellationTokenSource;
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _threads = new List<Thread>();
            _cancellationTokenSource = new CancellationTokenSource();
            for (var x = 0; x < Settings.Default.Threads; x++)
            {
                var thread = new Thread(() => new Worker.Node(Settings.Default.FFmpegPath, Settings.Default.ControllerApi).Run(_cancellationTokenSource.Token));
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

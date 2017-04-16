using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.WindowsService.Properties;
using FFmpegFarm.Worker;
using FFmpegFarm.Worker.ProgressUpdaters;

namespace FFmpegFarm.WindowsService
{
    public class FFmpegClient
    {
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task[] _tasks;

        public FFmpegClient(ILogger logger, CancellationTokenSource cancellationTokenSource)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (cancellationTokenSource == null) throw new ArgumentNullException(nameof(cancellationTokenSource));

            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void Start()
        {
            _logger.Information($"Starting service\n{Settings.Default.FFmpegPath}\n{Settings.Default.ControllerApi}\n{Settings.Default.Threads} threads.");
            _tasks = new Task[Settings.Default.Threads];
            var env =
                Settings.Default.EnvorimentVars
                    .Split(';')
                    .Select(l =>
                    {
                        var pair = l.Split('=');
                        return new KeyValuePair<string, string>(pair[0], pair[1]);
                    })
                    .ToDictionary(p => p.Key, p => p.Value);

            var apiUri = Settings.Default.ControllerApi;
            IApiWrapper apiWrapper = new ApiWrapper(apiUri, _logger, _cancellationTokenSource.Token);

            IProgressUpdater progressUpdater = Settings.Default.RabbitMqEnabled
                ? new RabbitMqProgressUpdater(Settings.Default.RabbitMqDsn, Settings.Default.RabbitMqQueueName)
                : new HttpProgressUpdater(apiWrapper) as IProgressUpdater;

            Node.PollInterval = TimeSpan.FromSeconds(10);
            for (var x = 0; x < Settings.Default.Threads; x++)
            {
                var task = Node.GetNodeTask(Settings.Default.FFmpegPath, apiUri, Settings.Default.FFmpegLogPath, env, _logger, progressUpdater,
                    _cancellationTokenSource.Token, apiWrapper);

                _tasks[x] = task;
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        public void Stop()
        {
            _logger.Information("Stopping service..");
            _cancellationTokenSource.Cancel();
            try
            {
                // ReSharper disable once MethodSupportsCancellation
                Task.WaitAll(_tasks, 20000);
                _logger.Information("Service stopped");
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Service cancelled tasks due to OnStop() called");
            }
            catch (Exception e)
            {
                if (!(e.InnerException?.GetType() == typeof(OperationCanceledException)
                    || e.InnerException?.GetType() == typeof(TaskCanceledException)))
                    throw;
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.Worker.Client;

namespace FFmpegFarm.Worker
{
    public class Node
    {
        private readonly string _ffmpegPath;
        private readonly Timer _timeSinceLastUpdate;
        private CancellationToken _cancellationToken;
        private TimeSpan _progress = TimeSpan.Zero;
        private Process _commandlineProcess;
        private readonly StringBuilder _output;
        private AudioTranscodingJob _currentJob;
        private static readonly int TimeOut = (int) TimeSpan.FromSeconds(20).TotalMilliseconds;
        private readonly ILogger _logger;
        private int? _threadId; // main thread id, used for logging in child threads.
        private int _progressSpinner;
        private const int ProgressSkip = 5;
        private readonly StatusClient _statusClient;
        private readonly AudioJobClient _audioJobClient;

        private Node(string ffmpegPath, string apiUri, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                throw new ArgumentNullException(nameof(ffmpegPath), "No path specified for FFmpeg binary. Missing configuration setting FfmpegPath");
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException(ffmpegPath);
            _ffmpegPath = ffmpegPath;
            if (string.IsNullOrWhiteSpace(apiUri))
                throw new ArgumentNullException(nameof(apiUri), "Api uri supplied");
            if(logger == null)
                throw new ArgumentNullException(nameof(logger));
            _timeSinceLastUpdate = new Timer(_ => TimeSinceLastUpdate_Elapsed(), null, -1, TimeOut);
            _output = new StringBuilder();
            _logger = logger;
            _audioJobClient = new AudioJobClient(apiUri);
            _statusClient = new StatusClient(apiUri);
            _logger.Debug("Node started...");
        }

        public static Task GetNodeTask(string ffmpegPath, string apiUri, ILogger logger, CancellationToken ct)
        {
            return new Task(() => new Node(ffmpegPath,apiUri,logger).Run(ct));
        }

        private void Run(CancellationToken ct)
        {
            try
            {
                _threadId = Thread.CurrentThread.ManagedThreadId;
                ct.ThrowIfCancellationRequested();
                _cancellationToken = ct;
                while (!ct.IsCancellationRequested)
                {
                    _currentJob = ApiWrapper(_audioJobClient.GetAsync, Environment.MachineName);
                    if (_currentJob == null)
                    {
                        Task.Delay(TimeSpan.FromSeconds(5), ct).GetAwaiter().GetResult();
                        continue;
                    }
                    ExecuteAudioTranscodingJob();
                    _output.Clear();
                }
                ct.ThrowIfCancellationRequested();
            }
            finally {
                _commandlineProcess?.Kill();
                if (_currentJob != null)
                {
                    _logger.Warn($"In progress job failed {_currentJob.JobCorrelationId}");
                    _currentJob.State = AudioTranscodingJobState.Failed;
                    // ReSharper disable once MethodSupportsCancellation
                    _statusClient.UpdateProgressAsync(_currentJob.ToBaseJob()).GetAwaiter().GetResult();
                }
                _logger.Debug("Cancel recived shutting down...");
            }
        }

        private void ExecuteAudioTranscodingJob()
        {
            _currentJob.MachineName = Environment.MachineName;
            _logger.Information($"New job recived {_currentJob.JobCorrelationId}", _threadId);
            using (_commandlineProcess = new Process())
            {
                _commandlineProcess.StartInfo = new ProcessStartInfo
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = _currentJob.Arguments
                };

                _logger.Debug($"ffmpeg arguments: {_commandlineProcess.StartInfo.Arguments}", _threadId);

                _commandlineProcess.OutputDataReceived += Ffmpeg_DataReceived;
                _commandlineProcess.ErrorDataReceived += Ffmpeg_DataReceived;
                
                _commandlineProcess.Start();
                _commandlineProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                _commandlineProcess.BeginErrorReadLine();

                _timeSinceLastUpdate.Change(TimeOut, TimeOut); // start

                _commandlineProcess.WaitForExit();

                if (FfmpegDetectedError())
                {
                    _currentJob.Failed = true;
                    _currentJob.Done = false;
                    _logger.Warn(_output.ToString());
                    _logger.Warn($"Job failed {_currentJob.JobCorrelationId}", _threadId);
                }
                else
                {
                    _currentJob.Done = _commandlineProcess.ExitCode == 0;
                    _logger.Information($"Job done {_currentJob.JobCorrelationId}", _threadId);
                }
                ApiWrapper(_statusClient.UpdateProgressAsync, _currentJob.ToBaseJob());
                
                _timeSinceLastUpdate.Change(-1, TimeOut); //stop
            }
        }

        private bool FfmpegDetectedError()
        {
            return Regex.IsMatch(_output.ToString(), @"\] Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        private void TimeSinceLastUpdate_Elapsed()
        {
            if (_commandlineProcess.HasExited)
                return;

            _commandlineProcess.Kill();
            _logger.Warn("Timed out..", _threadId);
        }

        private void Ffmpeg_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            _output.AppendLine(e.Data);

            var match = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (!match.Success) return;
            _timeSinceLastUpdate.Change(-1, TimeOut); //stop

            _progress = new TimeSpan(0, Convert.ToInt32(match.Groups[1].Value),
                Convert.ToInt32(match.Groups[2].Value), Convert.ToInt32(match.Groups[3].Value),
                Convert.ToInt32(match.Groups[4].Value) * 25);

            _currentJob.Progress = _progress.ToString();
            
            ApiWrapper(_statusClient.UpdateProgressAsync, _currentJob.ToBaseJob());

            if (_progressSpinner++%ProgressSkip == 0) // only print every 10 line
                _logger.Debug(_progress.ToString("c"), _threadId);

            _timeSinceLastUpdate.Change(TimeOut, TimeOut); //start
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private TRes ApiWrapper<TArg, TRes>(Func<TArg, CancellationToken, Task<TRes>> apiCall, TArg arg)
        {
            const int retryCount = 10;
            Exception exception = null;
            for (var x = 0; !_cancellationToken.IsCancellationRequested && x < retryCount; x++)
            {
                try
                {
                   return apiCall(arg, _cancellationToken).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    exception = e;
                }
                Thread.Sleep(1000);
            }
            _logger.Exception(exception ?? new Exception(nameof(ApiWrapper)), _threadId);
            return default(TRes);
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private void ApiWrapper<TArg>(Func<TArg, CancellationToken, Task> apiCall, TArg arg)
        {
            // work around since Task<void> is not allowed. 
            // THIS IS BROKEN FOR SOME REASON!
            // sorry, can't keep it dry... :'(
            /*
            ApiWrapper(
                (a,ct) => 
                new Task<object> (() =>
                {
                    apiCall(a, ct).GetAwaiter().GetResult();
                    return null;
                }), arg);
            */
            const int retryCount = 10;
            Exception exception = null;
            for (var x = 0; !_cancellationToken.IsCancellationRequested && x < retryCount; x++)
            {
                try
                {
                    apiCall(arg, _cancellationToken).GetAwaiter().GetResult();
                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                }
                Thread.Sleep(1000);
            }
            _logger.Exception(exception ?? new Exception(nameof(ApiWrapper)), _threadId);
            
        }
    }
}

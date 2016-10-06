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
        private readonly object _lock = new object();
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
            _timeSinceLastUpdate = new Timer(_ => KillProcess("Timed out"), null, -1, TimeOut);
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
                ct.Register(() => KillProcess("Canceled"));
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
                if (_currentJob != null)
                {
                    _logger.Warn($"In progress job re-queued {_currentJob.JobCorrelationId}");
                    _currentJob.State = AudioTranscodingJobState.Queued;
                    // ReSharper disable once MethodSupportsCancellation
                    _statusClient.UpdateProgressAsync(_currentJob.ToBaseJob()).GetAwaiter().GetResult(); // don't use wrapper since cancel has been called.
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

                if (_commandlineProcess.ExitCode != 0 || FfmpegDetectedError())
                {
                    _currentJob.Failed = true;
                    _currentJob.Done = false;
                    _logger.Warn(_output.ToString());
                    _logger.Warn($"Job failed {_currentJob.JobCorrelationId}", _threadId);
                }
                else
                {
                    _currentJob.Done = true;
                    _logger.Information($"Job {(_currentJob.Done.Value ? "Done" : "Canceled")} {_currentJob.JobCorrelationId}", _threadId);
                }
                ApiWrapper(_statusClient.UpdateProgressAsync, _currentJob.ToBaseJob());
                
                _timeSinceLastUpdate.Change(-1, TimeOut); //stop
                Monitor.Enter(_lock); // lock before dispose
            }
            _commandlineProcess = null;
            Monitor.Exit(_lock);
        }

        private bool FfmpegDetectedError()
        {

            return Regex.IsMatch(_output.ToString(), @"\] Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }
        
        private void KillProcess(string reason)
        {
            lock (_lock)
            {
                try
                {
                    if (_commandlineProcess == null || _commandlineProcess.HasExited)
                        return;

                    _commandlineProcess.Kill();
                    _logger.Warn($"Process kill, {reason}.", _threadId);
                }
                catch (Exception e)
                {
                    _logger.Exception(e);
                }
            }
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
        private TRes ApiWrapper<TRes>(Func<TRes> func)
        {
            const int retryCount = 3;
            Exception exception = null;
            SwaggerException swaggerException = null;
            for (var x = 0; !_cancellationToken.IsCancellationRequested && x < retryCount; x++)
            {
                #if DEBUGAPI
                var timer = new Stopwatch();
                timer.Start();
                #endif
                try
                {
                    return func();
                }
                catch (Exception e)
                {
                    exception = e;
                    swaggerException = e as SwaggerException;
                    #if DEBUGAPI
                    if (swaggerException != null)
                        _logger.Warn($"{swaggerException.StatusCode} : {Encoding.UTF8.GetString(swaggerException.ResponseData)}", _threadId);
                    _logger.Exception(e, _threadId);
                    #endif
                }
                #if DEBUGAPI
                finally
                {
                    _logger.Debug($"API call took {timer.ElapsedMilliseconds} ms", _threadId);
                    timer.Stop();
                }
                #endif
                Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken).GetAwaiter().GetResult();
            }
            if (swaggerException != null)
                _logger.Warn($"{swaggerException.StatusCode} : {Encoding.UTF8.GetString(swaggerException.ResponseData)}",_threadId);
            _logger.Exception(exception ?? new Exception(nameof(ApiWrapper)), _threadId);
            return default(TRes);
        }


        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private TRes ApiWrapper<TArg, TRes>(Func<TArg, CancellationToken, Task<TRes>> apiCall, TArg arg)
        {
            return ApiWrapper(() => apiCall(arg, CancellationToken.None).GetAwaiter().GetResult());
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private void ApiWrapper<TArg>(Func<TArg, CancellationToken, Task> apiCall, TArg arg)
        {
            ApiWrapper(
                new Func<object> (()=>{
                    apiCall(arg, CancellationToken.None).GetAwaiter().GetResult();
                    return null;
                }));
        }
    }
}

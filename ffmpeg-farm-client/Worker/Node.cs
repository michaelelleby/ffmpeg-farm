using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.Worker.Client;
using Nito.AsyncEx.Synchronous;

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
        private FFmpegTaskDto _currentTask;
        private static readonly int TimeOut = (int) TimeSpan.FromSeconds(20).TotalMilliseconds;
        private readonly ILogger _logger;
        private int? _threadId; // main thread id, used for logging in child threads.
        private int _progressSpinner;
        private const int ProgressSkip = 5;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private IApiWrapper _apiWrapper;
        
        private Node(string ffmpegPath, string apiUri, ILogger logger, IApiWrapper apiWrapper)
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
            _apiWrapper = apiWrapper;
            _logger.Debug("Node started...");
        }

        public static Task GetNodeTask(string ffmpegPath, 
            string apiUri, 
            ILogger logger, 
            CancellationToken ct,
            IApiWrapper apiWrapper = null)
        {
            var t = new Task(() => new Node(ffmpegPath,apiUri,logger,
                apiWrapper ?? new ApiWrapper(apiUri, logger, ct)).Run(ct));
            return t;
        }

        private void Run(CancellationToken ct)
        {
            try
            {
                _apiWrapper.ThreadId = _threadId = Thread.CurrentThread.ManagedThreadId;
                ct.ThrowIfCancellationRequested();
                ct.Register(() => KillProcess("Canceled"));
                _cancellationToken = ct;
                while (!_cancellationToken.IsCancellationRequested)
                {
                    _currentTask = _apiWrapper.GetNext(Environment.MachineName);
                    if (_currentTask == null)
                    {
                        Task.Delay(TimeSpan.FromSeconds(5), ct).WaitWithoutException();
                        continue;
                    }
                    ExecuteJob();
                    _output.Clear();
                }
                ct.ThrowIfCancellationRequested();
            }
            finally {
                if (_currentTask != null)
                {
                    _logger.Warn($"In progress job re-queued {_currentTask.Id.GetValueOrDefault(0)}");
                    _currentTask.State = FFmpegTaskDtoState.Queued;
                    // ReSharper disable once MethodSupportsCancellation
                    var model = new TaskProgressModel
                    {
                        MachineName = Environment.MachineName,
                        Id = _currentTask.Id.GetValueOrDefault(0),
                        Progress = TimeSpan.FromSeconds(_currentTask.Progress.GetValueOrDefault(0)).ToString("c"),
                        Failed = _currentTask.State == FFmpegTaskDtoState.Failed,
                        Done = _currentTask.State == FFmpegTaskDtoState.Done
                    };
                    _apiWrapper.UpdateProgress(model, true);
                }
                _logger.Debug("Cancel recived shutting down...");
            }
        }

        private void ExecuteJob()
        {
            _currentTask.HeartbeatMachineName = Environment.MachineName;
            _logger.Information($"New job recived {_currentTask.Id}", _threadId);
            _stopwatch.Start();
            using (_commandlineProcess = new Process())
            {
                _commandlineProcess.StartInfo = new ProcessStartInfo
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = _currentTask.Arguments
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
                    _currentTask.State = FFmpegTaskDtoState.Failed;
                    _logger.Warn($"Job failed {_currentTask.Id}."+
                        $"Time elapsed : {_stopwatch.Elapsed:g}"+
                        $"\n\tffmpeg process output:\n\n{_output}", _threadId);
                }
                else
                {
                    _currentTask.State = FFmpegTaskDtoState.Done;
                    _logger.Information($"Job done {_currentTask.Id}. Time elapsed : {_stopwatch.Elapsed:g}", _threadId);
                }
                var model = new TaskProgressModel
                {
                    MachineName = Environment.MachineName,
                    Id = _currentTask.Id.GetValueOrDefault(0),
                    Progress = TimeSpan.FromSeconds(_currentTask.Progress.GetValueOrDefault(0)).ToString("c"),
                    Failed = _currentTask.State == FFmpegTaskDtoState.Failed,
                    Done = _currentTask.State == FFmpegTaskDtoState.Done
                };
                _apiWrapper.UpdateProgress(model);
                
                _timeSinceLastUpdate.Change(-1, TimeOut); //stop
                Monitor.Enter(_lock); // lock before dispose
            }
            _commandlineProcess = null;
            _stopwatch.Stop();
            Monitor.Exit(_lock);
        }

        private bool FfmpegDetectedError()
        {
            // FFmpeg will return exit code 0 even when writing to the output the following:
            // Error while decoding stream #0:0: Invalid data found when processing input
            // so we need to check if there is a line beginning with the word Error

            return Regex.IsMatch(_output.ToString(), @"\] Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
                || Regex.IsMatch(_output.ToString(), @"^Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline);
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

            _currentTask.Progress = TimeSpan.Parse(_progress.ToString(), CultureInfo.InvariantCulture).TotalSeconds;

            _apiWrapper.UpdateProgress(new TaskProgressModel
            {
                Done = _currentTask.State == FFmpegTaskDtoState.Done,
                Failed = _currentTask.State == FFmpegTaskDtoState.Failed,
                Id = _currentTask.Id.GetValueOrDefault(0),
                MachineName = _currentTask.HeartbeatMachineName,
                Progress = TimeSpan.FromSeconds(_currentTask.Progress.Value).ToString("c")
            });

            if (_progressSpinner++%ProgressSkip == 0) // only print every 10 line
                _logger.Debug($"\n\tFile progress : {_progress:g}\n\tTime elapsed  : {_stopwatch.Elapsed:g}\n\tSpeed: {_progress.TotalMilliseconds/_stopwatch.ElapsedMilliseconds:P1}", _threadId);

            _timeSinceLastUpdate.Change(TimeOut, TimeOut); //start
        }
    }
}

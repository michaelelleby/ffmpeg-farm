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
        private readonly string _apiUri;
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
        private const int ProgressSkip = 10;

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
            _apiUri = apiUri;
            _timeSinceLastUpdate = new Timer(_ => TimeSinceLastUpdate_Elapsed(), null, -1, TimeOut);
            _output = new StringBuilder();
            _logger = logger;
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
                var jobClient = new AudioJobClient(_apiUri);
                while (!ct.IsCancellationRequested)
                {
                    var job = jobClient.GetAsync(Environment.MachineName, ct).Result;
                    if (job == null)
                    {
                        Task.Delay(TimeSpan.FromSeconds(5), ct).GetAwaiter().GetResult();
                        continue;
                    }
                    ExecuteAudioTranscodingJob(job);
                    _output.Clear();
                }
                ct.ThrowIfCancellationRequested();
            }
            finally { 
                // add cleanup if needed here...
                _logger.Debug("Cancel recived shutting down...");

            }
        }


        private void ExecuteAudioTranscodingJob(AudioTranscodingJob job)
        {
            _currentJob = job;
            _currentJob.MachineName = Environment.MachineName;
            _logger.Debug($"New job recived {job.JobCorrelationId}", _threadId);
            using (_commandlineProcess = new Process())
            {
                _commandlineProcess.StartInfo = new ProcessStartInfo
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = job.Arguments
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
                    _logger.Debug($"Job failed {job.JobCorrelationId}", _threadId);
                }
                else
                {
                    _currentJob.Done = _commandlineProcess.ExitCode == 0;
                    _logger.Debug($"Job done {job.JobCorrelationId}", _threadId);
                }
                var statusClient = new StatusClient(_apiUri);
                statusClient.UpdateProgressAsync(_currentJob.ToBaseJob(), _cancellationToken).GetAwaiter().GetResult();
                
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
            var statusClient = new StatusClient(_apiUri);
            statusClient.UpdateProgressAsync(_currentJob.ToBaseJob(), _cancellationToken).GetAwaiter().GetResult();

            if (_progressSpinner++%ProgressSkip == 0) // only print every 10 line
                _logger.Debug(_progress.ToString("g"), _threadId);

            _timeSinceLastUpdate.Change(TimeOut, TimeOut); //start
        }
    }
}

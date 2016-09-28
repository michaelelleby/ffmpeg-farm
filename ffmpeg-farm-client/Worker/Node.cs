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
        private Client.AudioTranscodingJob _currentJob;
        private static readonly int TimeOut = (int) TimeSpan.FromSeconds(20).TotalMilliseconds;
        
        public Node(string ffmpegPath, string apiUri)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                throw new ArgumentNullException(nameof(ffmpegPath), "No path specified for FFmpeg binary. Missing configuration setting FfmpegPath");
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException(ffmpegPath);
            _ffmpegPath = ffmpegPath;
            if (string.IsNullOrWhiteSpace(apiUri))
                throw new ArgumentNullException(nameof(apiUri), "Api uri supplied");
            _apiUri = apiUri;
            _timeSinceLastUpdate = new Timer(_ => TimeSinceLastUpdate_Elapsed(), null, -1, TimeOut);
            _output = new StringBuilder();
            Console.WriteLine("Node started...");
        }

        public async void Run(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _cancellationToken = ct;
            var jobClient = new Client.AudioJobClient(_apiUri);
            while (!ct.IsCancellationRequested)
            {
                var job = await jobClient.GetAsync(Environment.MachineName, ct);
                if (job == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    continue;
                }
                ExecuteAudioTranscodingJob(job);
                _output.Clear();
            }
            // add cleanup if needed here...
            ct.ThrowIfCancellationRequested();
        }


        private async void ExecuteAudioTranscodingJob(Client.AudioTranscodingJob job)
        {
            _currentJob = job;
            _currentJob.MachineName = Environment.MachineName;

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

                //Console.WriteLine(_commandlineProcess.StartInfo.Arguments);

                _commandlineProcess.OutputDataReceived += Ffmpeg_DataReceived;
                _commandlineProcess.ErrorDataReceived += Ffmpeg_DataReceived;
                
                _commandlineProcess.Start();
                _commandlineProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                _commandlineProcess.BeginErrorReadLine();

                _timeSinceLastUpdate.Change(0, TimeOut); // start

                _commandlineProcess.WaitForExit();

                if (FfmpegDetectedError())
                {
                    _currentJob.Failed = true;
                    _currentJob.Done = false;
                }
                else
                {
                    _currentJob.Done = _commandlineProcess.ExitCode == 0;
                }
                var statusClient = new StatusClient(_apiUri);
                await statusClient.PutAsync(_currentJob.ToBaseJob(), _cancellationToken);

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
            Console.WriteLine("Timed out..");
        }

        private async void Ffmpeg_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            _output.AppendLine(e.Data);

            var match = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (match.Success)
            {
                _timeSinceLastUpdate.Change(-1, TimeOut); //stop

                _progress = new TimeSpan(0, Convert.ToInt32(match.Groups[1].Value),
                    Convert.ToInt32(match.Groups[2].Value), Convert.ToInt32(match.Groups[3].Value),
                    Convert.ToInt32(match.Groups[4].Value) * 25);

                _currentJob.Progress = _progress.ToString();
                var statusClient = new StatusClient(_apiUri);
                await statusClient.PutAsync(_currentJob.ToBaseJob(),_cancellationToken);
                
                Console.WriteLine(_progress);

                _timeSinceLastUpdate.Change(0, TimeOut); //start
            }
        }
    }
}

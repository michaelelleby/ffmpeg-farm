using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ffmpeg_farm_client
{
    class Program
    {
        private static readonly System.Timers.Timer TimeSinceLastUpdate = new System.Timers.Timer(TimeSpan.FromSeconds(20).TotalMilliseconds);
        private static TimeSpan _progress = TimeSpan.Zero;
        private static Process _commandlineProcess;
        private static TranscodingJob CurrentJob;

        private static void Main(string[] args)
        {
            while (true)
            {
                _commandlineProcess = new Process();
                TranscodingJob receivedJob = null;

                try
                {
                    using (var client = new HttpClient())
                    {
                        HttpResponseMessage result =
                            client.GetAsync(string.Concat(ConfigurationManager.AppSettings["ServerUrl"],
                                "/transcodingjob")).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            string json = result.Content.ReadAsStringAsync().Result;
                            receivedJob = JsonConvert.DeserializeObject<TranscodingJob>(json);

                            _progress = new TimeSpan();
                        }
                    }

                    if (receivedJob != null)
                    {
                        ExecuteTranscodingJob(receivedJob);
                        continue;
                    }

                }
                catch (Exception)
                {
                    
                }

                // Wait 5 seconds before checking for a new job
                // this will prevent a loop taking 100% cpu
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private static void ExecuteTranscodingJob(TranscodingJob receivedJob)
        {
            TranscodingJob transcodingJob = (TranscodingJob)receivedJob;

            Console.WriteLine("Got job {0}", JsonConvert.SerializeObject(transcodingJob));

            CurrentJob = transcodingJob;

            _commandlineProcess.StartInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = ConfigurationManager.AppSettings["FfmpegPath"],
                Arguments = transcodingJob.Arguments
            };

            Console.WriteLine(_commandlineProcess.StartInfo.Arguments);

            _commandlineProcess.ErrorDataReceived += Ffmpeg_ErrorDataReceived;

            TimeSinceLastUpdate.Elapsed += TimeSinceLastUpdate_Elapsed;

            _commandlineProcess.Start();
            _commandlineProcess.BeginErrorReadLine();

            TimeSinceLastUpdate.Start();

            _commandlineProcess.WaitForExit();

            CurrentJob.Done = _commandlineProcess.ExitCode == 0;

            UpdateProgress();

            TimeSinceLastUpdate.Stop();
        }

        private static void TimeSinceLastUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            _commandlineProcess.Kill();
            Console.WriteLine("Timed out..");
        }

        private static void Ffmpeg_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            var match = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (match.Success)
            {
                // Restart timer
                TimeSinceLastUpdate.Stop();

                _progress = new TimeSpan(0, Convert.ToInt32(match.Groups[1].Value), Convert.ToInt32(match.Groups[2].Value), Convert.ToInt32(match.Groups[3].Value), Convert.ToInt32(match.Groups[4].Value) * 25);

                CurrentJob.Progress = _progress;
                UpdateProgress();

                Console.WriteLine(_progress);

                TimeSinceLastUpdate.Start();
            }
        }

        private static HttpResponseMessage UpdateProgress()
        {
            using (var client = new HttpClient())
            {
                return client.PutAsync(
                    new Uri(string.Concat(ConfigurationManager.AppSettings["ServerUrl"], "/transcodingjob")),
                    new StringContent(JsonConvert.SerializeObject(CurrentJob), Encoding.ASCII, "application/json"))
                    .Result;
            }
        }
    }

    public class TranscodingJob
    {
        public string Arguments { get; set; }
        public Guid JobCorrelationId { get; set; }
        public TimeSpan Progress { get; set; }
        public int Id { get; set; }
        public bool Done { get; set; }
    }
}

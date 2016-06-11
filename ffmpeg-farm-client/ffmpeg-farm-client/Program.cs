using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Contract;
using Newtonsoft.Json.Converters;

namespace ffmpeg_farm_client
{
    internal class Program
    {
        private static readonly System.Timers.Timer TimeSinceLastUpdate = new System.Timers.Timer(TimeSpan.FromSeconds(20).TotalMilliseconds);
        private static TimeSpan _progress = TimeSpan.Zero;
        private static Process _commandlineProcess;
        private static BaseJob CurrentJob;
        private static JsonSerializerSettings _jsonSerializerSettings;

        private static void Main(string[] args)
        {
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                                    {
                                        new IsoDateTimeConverter(),
                                        new StringEnumConverter()
                                    },
                TypeNameHandling = TypeNameHandling.All
            };

            while (true)
            {
                _commandlineProcess = new Process();
                object receivedJob = null;

                try
                {
                    using (var client = new HttpClient())
                    {
                        HttpResponseMessage result =
                            client.GetAsync(string.Concat(ConfigurationManager.AppSettings["ServerUrl"],
                                "/transcodingjob?machinename=" + Environment.MachineName)).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            string json = result.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                receivedJob = JsonConvert.DeserializeObject<BaseJob>(json,
                                    _jsonSerializerSettings);

                                _progress = new TimeSpan();
                            }
                        }
                    }

                    if (receivedJob != null)
                    {
                        Type jobType = receivedJob.GetType();
                        if (jobType == typeof(TranscodingJob) || jobType == typeof(MergeJob))
                        {
                            ExecuteTranscodingJob((TranscodingJob)receivedJob);
                        }
                        if (jobType == typeof(Mp4boxJob))
                        {
                            ExecuteMp4boxJob((Mp4boxJob)receivedJob);
                        }

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

        private static void ExecuteMp4boxJob(Mp4boxJob receivedJob)
        {
            string pathToMp4Box = ConfigurationManager.AppSettings["Mp4BoxPath"];
            if (string.IsNullOrWhiteSpace(pathToMp4Box)) throw new ArgumentNullException("Mp4BoxPath");
            if (!File.Exists(pathToMp4Box)) throw new FileNotFoundException(pathToMp4Box);

            CurrentJob = receivedJob;
            CurrentJob.MachineName = Environment.MachineName;

            _commandlineProcess.StartInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                FileName = pathToMp4Box,
                Arguments = receivedJob.Arguments
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

        private static void ExecuteTranscodingJob(TranscodingJob transcodingJob)
        {
            CurrentJob = transcodingJob;
            CurrentJob.MachineName = Environment.MachineName;

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
            if (!_commandlineProcess.HasExited)
            {
                _commandlineProcess.Kill();
                Console.WriteLine("Timed out..");
            }
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
                    new Uri(string.Concat(ConfigurationManager.AppSettings["ServerUrl"], "/status")),
                    new StringContent(JsonConvert.SerializeObject(CurrentJob, _jsonSerializerSettings), Encoding.ASCII, "application/json"))
                    .Result;
            }
        }
    }
}

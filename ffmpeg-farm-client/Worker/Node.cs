using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private enum Step
        {
            Work,
            Verify
        }
        private readonly object _lock = new object();
        private readonly string _logfilesPath;
        private readonly string _tmpfilesPath;
        private readonly Timer _timeSinceLastUpdate;
        private readonly Stopwatch _timeSinceLastProgressUpdate;
        private string _ffmpegPath;
        private string _stereotoolPath;
        private string _stereotoolLicenseParameter;
        private string _stereotoolPresetsPath;
        private CancellationToken _cancellationToken;
        private TimeSpan _progress = TimeSpan.Zero;
        private Process _commandlineProcess;
        private FFmpegTaskDto _currentTask;
        private Step _currentStep;
        private static readonly int TimeOut = (int) TimeSpan.FromMinutes(2).TotalMilliseconds;
        private readonly ILogger _logger;
        private int? _threadId; // main thread id, used for logging in child threads.
        private int _progressSpinner;
        private const int ProgressSkip = 5;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private IApiWrapper _apiWrapper;
        private readonly IDictionary<string, string> _envorimentVars;
        private LogFileWriter _taskLogFile;


        public static TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(20);

        private Node(string ffmpegPath, string stereotoolPath, string stereotoolLicensePath, string stereotoolPresetsPath, string apiUri, string logfilesPath, string tmpfilesPath, IDictionary<string,string> envorimentVars, ILogger logger, IApiWrapper apiWrapper)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                throw new ArgumentNullException(nameof(ffmpegPath), "No path specified for FFmpeg binary. Missing configuration setting FfmpegPath");
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("FFMPEG not found", ffmpegPath);
            _ffmpegPath = ffmpegPath;
            if (string.IsNullOrEmpty(stereotoolPath))
            {
                _stereotoolPath = null;
                _stereotoolLicenseParameter = "";
                _stereotoolPresetsPath = null;
            }
            else
            {
                if (!File.Exists(stereotoolPath))
                    throw new FileNotFoundException("Stereo tool not found", stereotoolPath);
                _stereotoolPath = stereotoolPath;

                if (File.Exists(stereotoolLicensePath)) //Test and dev runs Without the license, so we can leave this empty
                    _stereotoolLicenseParameter = " -k \"" + File.ReadAllText(stereotoolLicensePath) + "\"";

                if (!Directory.Exists(stereotoolPresetsPath) || !Directory.GetFiles(stereotoolPresetsPath)
                        .Any(p => p.EndsWith(".sts", StringComparison.InvariantCultureIgnoreCase)))
                    throw new ArgumentException($"No preset directory or presets found in {stereotoolPresetsPath}");
                _stereotoolPresetsPath = stereotoolPresetsPath;
                if (_stereotoolPresetsPath.EndsWith(@"\"))
                    _stereotoolPresetsPath = _stereotoolPresetsPath.Remove(_stereotoolPresetsPath.LastIndexOf(@"\", StringComparison.Ordinal), 1);
            }

            if (string.IsNullOrWhiteSpace(apiUri))
                throw new ArgumentNullException(nameof(apiUri), "Api uri supplied");
            if(logger == null)
                throw new ArgumentNullException(nameof(logger));
            if(envorimentVars == null)
                throw new ArgumentNullException(nameof(envorimentVars));
            _timeSinceLastUpdate = new Timer(_ => KillProcess("Timed out"), null, -1, TimeOut);
            _logger = logger;
            _apiWrapper = apiWrapper;
            _logger.Debug("Node started...");
            _logfilesPath = logfilesPath;

            if (string.IsNullOrEmpty(tmpfilesPath)) {
                _tmpfilesPath = Path.GetTempPath();
            }
            else {
                if (!Directory.Exists(tmpfilesPath))
                    Directory.CreateDirectory(tmpfilesPath);
                _tmpfilesPath = tmpfilesPath;
             };

            _envorimentVars = envorimentVars;
            _timeSinceLastProgressUpdate = new Stopwatch();
        }

        public static Task GetNodeTask(string ffmpegPath, 
            string stereotoolPath,
            string stereotoolLicensePath,
            string stereotoolPresetsPath,
            string apiUri, 
            string logfilesPath,
            string tmpfilesPath,
            IDictionary<string, string> envorimentVars,
            ILogger logger,
            CancellationToken ct,
            IApiWrapper apiWrapper = null)
        {

            var t = Task.Run(() => 
            new Node(ffmpegPath, stereotoolPath, stereotoolLicensePath, stereotoolPresetsPath, apiUri, logfilesPath, tmpfilesPath, envorimentVars, logger, apiWrapper ?? new ApiWrapper(apiUri, logger, ct)).Run(ct), ct);
            return t;
        }

        private void Run(CancellationToken ct)
        {
            _threadId = Thread.CurrentThread.ManagedThreadId;
            using (var processLogFile = new LogFileWriter(GetProcessLogOutputFileName(_logfilesPath, Environment.MachineName)))
            {
                try
                {
                    _apiWrapper.ThreadId = _threadId;
                    ct.ThrowIfCancellationRequested();
                    ct.Register(() => KillProcess("Canceled"));
                    _cancellationToken = ct;
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        _currentTask = null;
                        _currentTask = _apiWrapper.GetNext(Environment.MachineName);
                        if (_currentTask == null)
                        {
                            Task.Delay(PollInterval, ct).WaitWithoutException(ct);
                            continue;
                        }
                        try
                        {
                            // we let the server override default ffmpeg
                            if (!string.IsNullOrEmpty(_currentTask.FfmpegExePath) && File.Exists(_currentTask.FfmpegExePath))
                                _ffmpegPath = _currentTask.FfmpegExePath;
                            ExecuteJob();
                        }
                        catch (Exception e)
                        {
                            var text = $"Job failed {_currentTask.Id}, with error: {e}" +
                                       $"\n\tTime elapsed : {_stopwatch.Elapsed:g}";

                            _logger.Warn(text, _threadId);
                            processLogFile.WriteLine(DateTime.Now.ToString());
                            processLogFile.WriteLine(text);
                            try
                            {
                                Monitor.Enter(_lock);
                                if (_currentTask != null)
                                {
                                    _currentTask.State = FFmpegTaskDtoState.Failed;
                                    UpdateTask(_currentTask);
                                }
                                Monitor.Exit(_lock);
                            }
                            catch (Exception exception)
                            {
                                processLogFile.WriteLine(DateTime.Now.ToString());
                                processLogFile.WriteLine(exception.Message + exception.StackTrace);
                            }
                        }
                    }
                    ct.ThrowIfCancellationRequested();
                }
                finally
                {
                    if (_currentTask != null)
                    {
                        try
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
                        catch (Exception e)
                        {
                            // Prevent uncaught exceptions in the finally {} block
                            // since this will bring down the entire worker service
                            _logger.Exception(e, _threadId, "Run");
                            processLogFile.WriteLine(DateTime.Now.ToString());
                            processLogFile.WriteLine(e.Message + e.StackTrace);
                        }
                    }
                    _logger.Debug("Cancel recived shutting down...");
                }
            }
        }

        private void ExecuteJob()
        {
            _currentTask.HeartbeatMachineName = Environment.MachineName;
            _logger.Information($"New job recived {_currentTask.Id}", _threadId);
            _stopwatch.Restart();

            bool acquiredLock = false;

            using (_taskLogFile = new LogFileWriter(GetTaskLogOutputFileName(_logfilesPath, _currentTask)))
            {
                try
                {
                    if (_currentTask.Arguments.Contains("{StereoToolPath}") && _stereotoolPath == null)
                    {
                        throw new Exception("Stereo tool is unconfigured. Unable to execute current task.");
                    }

                    var destDir = Path.GetDirectoryName(_currentTask.DestinationFilename);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    int exitCode = -1;
                    using (_commandlineProcess = new Process())
                    {
                        _currentStep = Step.Work;
                        string outputFullPath = string.Empty;
                        var useCmdExe = _currentTask.Arguments.Contains("{FFMpegPath}") || _currentTask.Arguments.Contains("{StereoToolPath}"); // Use cmd.exe if either path to ffmpeg or stereotool is present.

                        string arguments = _currentTask.Arguments
                            .Replace("{FFMpegPath}", _ffmpegPath)
                            .Replace("{StereoToolPath}", _stereotoolPath)
                            .Replace("{StereoToolPresetsPath}", _stereotoolPresetsPath)
                            .Replace("{StereoToolLicense}", _stereotoolLicenseParameter);

                        // <TEMP> as output filename means we should transcode the file to the local disk
                        // and move it to destination path after it is done transcoding
                        if (arguments.IndexOf(@"|TEMP|", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            //It is important that the file extention is the correct type, ffmpeg will not like the .tmp extension for its outputs
                            outputFullPath = Path.Combine(_tmpfilesPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + Path.GetExtension(_currentTask.DestinationFilename));

                            arguments = arguments.Replace(@"|TEMP|", outputFullPath);
                        }
                        else
                        {
                            outputFullPath = _currentTask.DestinationFilename;
                        }

                        _commandlineProcess.StartInfo = new ProcessStartInfo
                        {
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            FileName = useCmdExe ? $"{Environment.SystemDirectory}{Path.DirectorySeparatorChar}cmd.exe" : _ffmpegPath,
                            Arguments = $"{(useCmdExe ? "/c " : "")}{arguments}"
                        };
                        var env = _commandlineProcess.StartInfo.Environment;
                        foreach (var e in _envorimentVars)
                        {
                            env[e.Key] = e.Value;
                        }

                        string debugCmdString = $"{(useCmdExe ? "cmd.exe" : "ffmpeg")} arguments: {_commandlineProcess.StartInfo.Arguments}";
                        _logger.Debug(debugCmdString, _threadId);
                        _taskLogFile.WriteLine(debugCmdString + Environment.NewLine);

                        _commandlineProcess.OutputDataReceived += Ffmpeg_DataReceived;
                        _commandlineProcess.ErrorDataReceived += Ffmpeg_DataReceived;

                        _commandlineProcess.Start();
                        _commandlineProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                        _commandlineProcess.BeginErrorReadLine();

                        _timeSinceLastUpdate.Change(TimeOut, TimeOut); // start

                        _commandlineProcess.WaitForExit();

                        _timeSinceLastProgressUpdate.Stop();

                        PostProgressUpdate();

                        // Disable timer to prevet accidentally aborting the ffmpeg task
                        // due to moving the file taking several seconds without any
                        // status updates
                        _timeSinceLastUpdate.Change(Timeout.Infinite, Timeout.Infinite);

                        if (string.Compare(outputFullPath, _currentTask.DestinationFilename, StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            //Alternative "move", with forced overwrite.
                            File.Copy(outputFullPath, _currentTask.DestinationFilename, true);
                            File.Delete(outputFullPath);
                        }

                        _commandlineProcess.OutputDataReceived -= Ffmpeg_DataReceived;
                        _commandlineProcess.ErrorDataReceived -= Ffmpeg_DataReceived;

                        exitCode = _commandlineProcess.ExitCode;
                    }
                    _commandlineProcess = null;

                    // VerifyOutput is a bool? and will default to false, if it is null
                    if (_currentTask.VerifyOutput.GetValueOrDefault() && exitCode == 0)
                    {
                        // Check that output file is not corrupt, meaning FFmpeg can read the file

                        using (_commandlineProcess = new Process())
                        {
                            _currentStep = Step.Verify;
                            _commandlineProcess.StartInfo = new ProcessStartInfo
                            {
                                RedirectStandardError = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                FileName = _ffmpegPath,
                                Arguments = $@"-xerror -i ""{_currentTask.DestinationFilename}"" -f null -"
                            };

                            _logger.Debug($"ffmpeg arguments: {_commandlineProcess.StartInfo.Arguments}", _threadId);

                            _commandlineProcess.OutputDataReceived += Ffmpeg_DataReceived;
                            _commandlineProcess.ErrorDataReceived += Ffmpeg_DataReceived;

                            _commandlineProcess.Start();
                            _commandlineProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
                            _commandlineProcess.BeginErrorReadLine();

                            _timeSinceLastUpdate.Change(TimeOut, TimeOut); // start

                            _commandlineProcess.WaitForExit();

                            _commandlineProcess.OutputDataReceived -= Ffmpeg_DataReceived;
                            _commandlineProcess.ErrorDataReceived -= Ffmpeg_DataReceived;

                            // Disable timer to prevent trying to kill a process
                            // which has already exited
                            _timeSinceLastUpdate.Change(Timeout.Infinite, Timeout.Infinite);

                            exitCode = _commandlineProcess.ExitCode;
                        }
                        _commandlineProcess = null;
                    }

                    if (exitCode != 0)// || _taskLogFile.FfmpegDetectedError())
                    {
                        _currentTask.State = FFmpegTaskDtoState.Failed;
                        _logger.Warn($"Job failed {_currentTask.Id}." +
                                     $"Time elapsed : {_stopwatch.Elapsed:g}" +
                                     $"\n\tffmpeg process output:\n\n{_taskLogFile.GetOutput()}", _threadId);
                    }
                    else
                    {
                        _currentTask.State = FFmpegTaskDtoState.Done;
                        _logger.Information($"Job done {_currentTask.Id}. Time elapsed : {_stopwatch.Elapsed:g}",
                            _threadId);
                    }
                    UpdateTask(_currentTask);

                    Monitor.Enter(_lock, ref acquiredLock); // lock before dispose
                }
                catch (Exception e)
                {
                    _logger.Exception(e, _threadId, "ExecuteJob");
                    _taskLogFile.WriteLine(e.Message + e.StackTrace);
                    _currentTask.State = FFmpegTaskDtoState.Failed;
                    UpdateTask(_currentTask);
                }
                finally
                {
                    _stopwatch.Stop();

                    // Cleanup when job fails
                    try
                    {
                        if (_currentTask.State.GetValueOrDefault() == FFmpegTaskDtoState.Failed
                            && File.Exists(_currentTask.DestinationFilename))
                        {
                            File.Delete(_currentTask.DestinationFilename);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Exception(e, _threadId, "ExecuteJob");
                        _taskLogFile.WriteLine(e.Message + e.StackTrace);
                    }

                    if (acquiredLock)
                    {
                        _commandlineProcess = null;
                        _currentTask = null;

                        Monitor.Exit(_lock);
                    }
                }
            }
        }

        private string GetTaskLogOutputFileName(string logFilePath, FFmpegTaskDto task)
        {
            try
            {
                var path = Path.Combine(logFilePath, task.Started.Value.ToString("yyyy"), task.Started.Value.ToString("MM"), task.Started.Value.ToString("dd"));
                Directory.CreateDirectory(path);
                var logPath = Path.Combine(path, $@"task_{task.Id}_output.txt");

                return logPath;
            }
            catch
            {
                // Prevent this from ever crashing the worker
                return Path.Combine(_tmpfilesPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + ".txt");
            }
        }

        private string GetProcessLogOutputFileName(string logFilePath, string machineName)
        {
            DateTime logStartTime = DateTime.Now;
            string logStartTimeString = logStartTime.ToString("yyyy-M-dd_HH.mm.ss");
            try
            {
                var path = Path.Combine(logFilePath, logStartTime.ToString("yyyy"), logStartTime.ToString("MM"), logStartTime.ToString("dd"));
                Directory.CreateDirectory(path);
                var logPath = Path.Combine(path, $@"MachineLog_{machineName}_{logStartTimeString}_thread_{_threadId}.txt");

                return logPath;
            }
            catch
            {
                // Prevent this from ever crashing the worker
                return Path.Combine(_tmpfilesPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()) + $@"_{machineName}_{logStartTimeString}_thread_{_threadId}.txt");
            }
        }

        private void UpdateTask(FFmpegTaskDto task)
        {
            var model = new TaskProgressModel
            {
                MachineName = Environment.MachineName,
                Id = task.Id.GetValueOrDefault(0),
                Progress = TimeSpan.FromSeconds(task.Progress.GetValueOrDefault(0)).ToString("c"),
                VerifyProgress = TimeSpan.FromSeconds(task.VerifyProgress.GetValueOrDefault(0)).ToString("c"),
                Failed = task.State == FFmpegTaskDtoState.Failed,
                Done = task.State == FFmpegTaskDtoState.Done
            };
            _apiWrapper.UpdateProgress(model);
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
                    _logger.Exception(e, null, "KillProcess");
                }
            }
        }

        private void Ffmpeg_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            _taskLogFile.WriteLine(e.Data);

            var match = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
            if (!match.Success) return;

            _timeSinceLastUpdate.Change(Timeout.Infinite, Timeout.Infinite); //stop

            // TODO Change hardcoded value 25 to actual FPS of input video
            // and handle audio not having any FPS value
            _progress = new TimeSpan(0, Convert.ToInt32(match.Groups[1].Value),
                Convert.ToInt32(match.Groups[2].Value), Convert.ToInt32(match.Groups[3].Value),
                Convert.ToInt32(match.Groups[4].Value) * 25);

            var value = TimeSpan.Parse(_progress.ToString(), CultureInfo.InvariantCulture).TotalSeconds;
            switch (_currentStep)
            {
                case Step.Work:
                    _currentTask.Progress = value;
                    break;
                case Step.Verify:
                    _currentTask.VerifyProgress = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_timeSinceLastProgressUpdate.IsRunning == false || _timeSinceLastProgressUpdate.ElapsedMilliseconds > 10000)
            {
                if (_timeSinceLastProgressUpdate.IsRunning)
                    _timeSinceLastProgressUpdate.Stop();

                PostProgressUpdate();

                _timeSinceLastProgressUpdate.Restart();
            }

            if (_progressSpinner++%ProgressSkip == 0) // only print every 10 line
                _logger.Debug($"\n\tFile progress : {_progress:g}\n\tTime elapsed  : {_stopwatch.Elapsed:g}\n\tSpeed: {_progress.TotalMilliseconds/_stopwatch.ElapsedMilliseconds:P1}", _threadId);

            _timeSinceLastUpdate.Change(TimeOut, TimeOut); //start
        }

        private void PostProgressUpdate()
        {
            try
            {
                Response state = _apiWrapper.UpdateProgress(new TaskProgressModel
                {
                    Done = _currentTask.State == FFmpegTaskDtoState.Done,
                    Failed = _currentTask.State == FFmpegTaskDtoState.Failed,
                    Id = _currentTask.Id.GetValueOrDefault(0),
                    MachineName = _currentTask.HeartbeatMachineName,
                    Progress = TimeSpan.FromSeconds(_currentTask.Progress.GetValueOrDefault(0)).ToString("c"),
                    VerifyProgress =
                        _currentTask.VerifyProgress.HasValue ? TimeSpan.FromSeconds(_currentTask.VerifyProgress.Value).ToString("c") : null
                });

                if (state == Response.Canceled)
                {
                    KillProcess("Canceled from ffmpeg server");
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}

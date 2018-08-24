using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.Worker;
using FFmpegFarm.Worker.Client;
using Moq;
using NUnit.Framework;

namespace Worker.Test
{
    [TestFixture]
    public class NodeTest
    {

        private static string stereotoolCfgPath =>
            ConfigurationManager.AppSettings["StereoToolCfgPath"] ?? @"C:\Temp\ffmpegfarm\stereotool\";
        private static readonly IDictionary<string,string> envs = new Dictionary<string, string>();
        private static readonly string destinationFilename = @"C:\temp\unit-test\lol.mp4";
        private static readonly bool writeOutputFileToDisk = false;
        private static readonly string ffmpegPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "ffmpeg.exe");
        private static readonly string stereotoolPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "stereo_tool_cmd.exe");
        private static readonly string stereotoolPresetsPath = $"{stereotoolCfgPath}presets"; // Folder containing your presets.
        private static readonly string stereotoolLicensePath = $"{stereotoolCfgPath}license"; // File containing your license key.

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!Directory.Exists(stereotoolPresetsPath))
            {
                throw new DirectoryNotFoundException($"Couldn't load stereo tool presets {stereotoolPresetsPath}");
            }
            if (!File.Exists(stereotoolLicensePath))
            {
                throw new FileNotFoundException("Couldn't load stereo tool license", stereotoolLicensePath);
            }
            if (!File.Exists(stereotoolPath))
            {
                throw new FileNotFoundException("Couldn't load stereo_tool_cmd, check download step in csproj", stereotoolPath);
            }
            if (!File.Exists(ffmpegPath))
            {
                throw new FileNotFoundException("Couldn't load ffmpeg, check download step in csproj", ffmpegPath);

            }
        }

        [Test]
        public async Task CanWaveAndStereoToolPreset()
        {
            // Arrange
            var srcFilePath = $"{TestContext.CurrentContext.TestDirectory}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Test.wav";
            var stereoToolOutputPath = $"{Path.GetDirectoryName(destinationFilename)}{Path.DirectorySeparatorChar}stereotooloutput.wav";

            var dto = new FFmpegTaskDto
            {
                Arguments = $"{{FFMpegPath}} -i \"{srcFilePath}\" -f wav -hide_banner -loglevel info - | \"{{StereoToolPath}}\" - {(writeOutputFileToDisk ? $"\"{stereoToolOutputPath}\"" : "NUL")} -s \"{{StereoToolPresetsPath}}{Path.DirectorySeparatorChar}Ossian_Px_7threads_ny.sts\" -k \"{{StereoToolLicense}}\" -q",
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = stereoToolOutputPath
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);
            
            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, stereotoolPath, stereotoolLicensePath, stereotoolPresetsPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", null, envs, mockLogger.Object, cancelSource.Token, apiWrapper);

            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }
            catch (OperationCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }

            // Assert
            Assert.That(apiWrapper.IsDone, Is.True);
        }


        [Test]
        public async Task CanEncodeAudio()
        {
            // Arrange
            var srcFilePath = $"{TestContext.CurrentContext.TestDirectory}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Test.wav";
            //var srcFilePath = @"C:\Temp\unit-test\stereotooloutput.wav";
            var destFilePath = writeOutputFileToDisk ? destinationFilename : "NUL";
            var dto = new FFmpegTaskDto
            {
                //Arguments = $"-xerror -i {srcFilePath} -f mp4 -b:a 192k -y {destFilePath}",
                Arguments = $"-xerror -i {srcFilePath} -f mp3 -y {destFilePath}",
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = destinationFilename
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);
            
            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, stereotoolPath, stereotoolLicensePath, stereotoolPresetsPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", null, envs, mockLogger.Object, cancelSource.Token, apiWrapper);

            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }
            catch (OperationCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }

            // Assert
            Assert.That(apiWrapper.IsDone, Is.True);
        }

        [Test]
        public async Task FailsIfSourceFileIsInvalid()
        {
            // Arrange
            var dto = new FFmpegTaskDto
            {
                Arguments =
                    string.Format("-xerror -i {0}{1}Data{1}Test_invalid.wav -f mp4 -b:a 192k -y {2}", TestContext.CurrentContext.TestDirectory,
                        Path.DirectorySeparatorChar,
                        writeOutputFileToDisk ? destinationFilename : "NUL"),
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = destinationFilename
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);

            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, stereotoolPath, stereotoolLicensePath, stereotoolPresetsPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", null, envs, mockLogger.Object, cancelSource.Token, apiWrapper);

            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }
            catch (OperationCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }

            // Assert
            Assert.That(apiWrapper.IsFailed, Is.True);
        }

        [Test]
        public async Task ShouldNotSetTaskToQueuedIfTaskIsFailedAndWorkerIsStopped()
        {
            // Arrange
            var dto = new FFmpegTaskDto
            {
                Arguments =
                    string.Format("-xerror -i {0}{1}Data{1}Test_invalid.wav -f mp4 -b:a 192k -y {2}", TestContext.CurrentContext.TestDirectory,
                        Path.DirectorySeparatorChar,
                        writeOutputFileToDisk ? destinationFilename : "NUL"),
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = destinationFilename
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);

            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, stereotoolPath, stereotoolLicensePath, stereotoolPresetsPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", null, envs, mockLogger.Object, cancelSource.Token, apiWrapper);

            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }
            catch (OperationCanceledException)
            {
                // Ignore task was cancelled, because we cancel it in FakeApiWrapper.UpdateProgress()
            }

            // Assert
            Assert.That(apiWrapper.IsFailed, Is.True);
            Assert.That(apiWrapper.IsDone, Is.False);
        }

        [TearDown]
        public void CleanUp()
        {
            var destDir = Path.GetDirectoryName(destinationFilename);
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

        }

        class FakeApiWrapper : IApiWrapper
        {
            private readonly CancellationTokenSource _token;
            public bool IsDone { get; private set; }

            public bool IsFailed { get; private set; }

            public FakeApiWrapper(CancellationTokenSource token)
            {
                if (token == null) throw new ArgumentNullException(nameof(token));

                _token = token;

                Tasks = new Stack<FFmpegTaskDto>();
            }

            public Stack<FFmpegTaskDto> Tasks { get; }

            public FFmpegTaskDto GetNext(string machineName)
            {
                return Tasks.Count > 0 ? Tasks.Pop() : null;
            }

            public Response UpdateProgress(TaskProgressModel model, bool ignoreCancel = false)
            {
                IsDone = model.Done;
                IsFailed = model.Failed;

                if (IsDone || IsFailed)
                {
                    _token.Cancel();
                }

                if (IsDone)
                    return Response.Done;
                if (IsFailed)
                    return Response.Failed;

                return Response.InProgress;
            }

            public int? ThreadId { get; set; }
        }
    }
}

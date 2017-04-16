using System;
using System.Collections.Generic;
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
        private static readonly IDictionary<string,string> envs = new Dictionary<string, string>();
        [Test]
        public async Task CanEncodeAudio()
        {
            // Arrange
            string ffmpegPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "ffmpeg.exe");
            var dto = new FFmpegTaskDto
            {
                Arguments =
                    string.Format("-xerror -i {0}{1}Data{1}Test.wav -f mp4 -b:a 192k -y NUL", TestContext.CurrentContext.TestDirectory,
                        Path.DirectorySeparatorChar),
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = "C:\\temp\\unit-test\\lol.mp4"
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IProgressUpdater> mockProgressUpdater = new Mock<IProgressUpdater>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);

            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", envs, mockLogger.Object, mockProgressUpdater.Object, cancelSource.Token, apiWrapper);

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
            string ffmpegPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "ffmpeg.exe");
            var dto = new FFmpegTaskDto
            {
                Arguments =
                    string.Format("-xerror -i {0}{1}Data{1}Test_invalid.wav -f mp4 -b:a 192k -y NUL", TestContext.CurrentContext.TestDirectory,
                        Path.DirectorySeparatorChar),
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = "C:\\temp\\unit-test\\lol.mp4"
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IProgressUpdater> mockProgressUpdater = new Mock<IProgressUpdater>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);

            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", envs, mockLogger.Object, mockProgressUpdater.Object, cancelSource.Token, apiWrapper);

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
            string ffmpegPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "ffmpeg.exe");
            var dto = new FFmpegTaskDto
            {
                Arguments =
                    string.Format("-xerror -i {0}{1}Data{1}Test_invalid.wav -f mp4 -b:a 192k -y NUL", TestContext.CurrentContext.TestDirectory,
                        Path.DirectorySeparatorChar),
                FfmpegJobsId = 1,
                Id = 10,
                State = FFmpegTaskDtoState.InProgress,
                DestinationFilename = "C:\\temp\\unit-test\\lol.mp4"
            };

            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<IProgressUpdater> mockProgressUpdater = new Mock<IProgressUpdater>();
            var cancelSource = new CancellationTokenSource();
            var apiWrapper = new FakeApiWrapper(cancelSource);

            apiWrapper.Tasks.Push(dto);

            // Act
            var task = Node.GetNodeTask(ffmpegPath, "TEST URL NOT IMPORTANT NOT USED", "LOGFILE OUTPUT PATH NOT USED", envs, mockLogger.Object, mockProgressUpdater.Object, cancelSource.Token, apiWrapper);

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

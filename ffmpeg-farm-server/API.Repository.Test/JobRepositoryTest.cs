using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Contract;
using Moq;
using NUnit.Framework;

namespace API.Repository.Test
{
    [TestFixture, Ignore("test")]
    public class JobRepositoryTest
    {
        private SqlFixture _fixture;

        [SetUp]
        public void SetupDatabase()
        {
            _fixture = new SqlFixture();
        }

        [TearDown]
        public void CleanupDatabase()
        {
            if (_fixture != null)
            {
                _fixture.Dispose();
                _fixture = null;
            }
        }

        [Test]
        public void GetNextJobShouldReturnNextTask()
        {
            // Arrange
            const string machinename = "TESTMACHINENAME";

            Mock<IHelper> helper = new Mock<IHelper>();
            IHardSubtitlesJobRepository repository = new HardSubtitlesJobRepository(helper.Object, _fixture.ConnectionString);
            IJobRepository sut = new JobRepository(helper.Object);

            helper.Setup(m => m.GetConnection())
                .Returns(() => new SqlConnection(_fixture.ConnectionString));

            repository.Add(new HardSubtitlesJobRequest
            {
                DestinationFilename = "testoutputfilename",
                Needed = DateTimeOffset.Now,
                OutputFolder = "testoutputfolder",
                SubtitlesFilename = "testsubtitlefilename",
                VideoSourceFilename = "testvideofilename"
            }, new List<FFmpegJob>
            {
                new HardSubtitlesJob
                {
                    Needed = DateTimeOffset.Now,
                    Arguments = "testarguments",
                    JobCorrelationId = Guid.NewGuid(),
                    SourceFilename = "testsourcefilename"
                }
            });

            // Act
            var result = sut.GetNextJob(machinename);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetNextJobShouldReturnNullIfMachineAlreadyHasAHardSubtitlesJobActive()
        {
            // Arrange
            const string machinename = "TESTMACHINENAME";

            Mock<IHelper> helper = new Mock<IHelper>();
            IAudioJobRepository audioJobRepository = new AudioJobRepository(helper.Object);
            IHardSubtitlesJobRepository hardSubtitlesJobRepository = new HardSubtitlesJobRepository(helper.Object, _fixture.ConnectionString);
            IJobRepository sut = new JobRepository(helper.Object);

            helper.Setup(m => m.GetConnection())
                .Returns(() => new SqlConnection(_fixture.ConnectionString));

            hardSubtitlesJobRepository.Add(new HardSubtitlesJobRequest
            {
                DestinationFilename = "testoutputfilename",
                Needed = DateTimeOffset.Now,
                OutputFolder = "testoutputfolder",
                SubtitlesFilename = "testsubtitlefilename",
                VideoSourceFilename = "testvideofilename"
            }, new List<FFmpegJob>
            {
                new HardSubtitlesJob
                {
                    Needed = DateTimeOffset.Now,
                    Arguments = "testarguments",
                    JobCorrelationId = Guid.NewGuid(),
                    SourceFilename = "testsourcefilename"
                }
            });

            audioJobRepository.Add(new AudioJobRequest
            {
                DestinationFilename = "testoutputfilename",
                Needed = DateTimeOffset.Now,
                OutputFolder = "testoutputfolder",
                SourceFilenames = new List<string>
                {
                    "input.wav"
                },
                Targets = new[]
                {
                    new AudioDestinationFormat
                    {
                        AudioCodec = AudioCodec.MP3,
                        Bitrate = 192,
                        Channels = Channels.Stereo,
                        Format = ContainerFormat.MP4
                    }
                }
            }, new List<AudioTranscodingJob>
            {
                new AudioTranscodingJob
                {
                    Needed = DateTimeOffset.Now,
                    Arguments = "testarguments",
                    JobCorrelationId = Guid.NewGuid(),
                    SourceFilename = "testsourcefilename"
                }
            });

            // Act
            var job = sut.GetNextJob(machinename);
            Assert.That(job, Is.Not.Null);

            sut.SaveProgress(job.Id, false, false, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), machinename, DateTimeOffset.Now);

            var result = sut.GetNextJob(machinename);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetNextJobShouldReturnNewJobIfMachineOnlyHasAudioJobsActive()
        {
            // Arrange
            const string machinename = "TESTMACHINENAME";

            Mock<IHelper> helper = new Mock<IHelper>();
            IAudioJobRepository audioJobRepository = new AudioJobRepository(helper.Object);
            IHardSubtitlesJobRepository hardSubtitlesJobRepository = new HardSubtitlesJobRepository(helper.Object, _fixture.ConnectionString);
            IJobRepository sut = new JobRepository(helper.Object);

            helper.Setup(m => m.GetConnection())
                .Returns(() => new SqlConnection(_fixture.ConnectionString));

            audioJobRepository.Add(new AudioJobRequest
            {
                DestinationFilename = "testoutputfilename",
                Needed = DateTimeOffset.Now,
                OutputFolder = "testoutputfolder",
                SourceFilenames = new List<string>
                {
                    "input.wav"
                },
                Targets = new[]
                {
                    new AudioDestinationFormat
                    {
                        AudioCodec = AudioCodec.MP3,
                        Bitrate = 192,
                        Channels = Channels.Stereo,
                        Format = ContainerFormat.MP4
                    }
                }
            }, new List<AudioTranscodingJob>
            {
                new AudioTranscodingJob
                {
                    Needed = DateTimeOffset.Now,
                    Arguments = "testarguments",
                    JobCorrelationId = Guid.NewGuid(),
                    SourceFilename = "testsourcefilename"
                }
            });

            audioJobRepository.Add(new AudioJobRequest
            {
                DestinationFilename = "testoutputfilename",
                Needed = DateTimeOffset.Now,
                OutputFolder = "testoutputfolder",
                SourceFilenames = new List<string>
                {
                    "input.wav"
                },
                Targets = new[]
                {
                    new AudioDestinationFormat
                    {
                        AudioCodec = AudioCodec.MP3,
                        Bitrate = 192,
                        Channels = Channels.Stereo,
                        Format = ContainerFormat.MP4
                    }
                }
            }, new List<AudioTranscodingJob>
            {
                new AudioTranscodingJob
                {
                    Needed = DateTimeOffset.Now,
                    Arguments = "testarguments",
                    JobCorrelationId = Guid.NewGuid(),
                    SourceFilename = "testsourcefilename"
                }
            });

            // Act
            var job = sut.GetNextJob(machinename);
            sut.SaveProgress(job.Id, false, false, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), machinename, DateTimeOffset.Now);

            var result = sut.GetNextJob(machinename);

            // Assert
            Assert.That(result, Is.Not.Null);
        }
    }
}
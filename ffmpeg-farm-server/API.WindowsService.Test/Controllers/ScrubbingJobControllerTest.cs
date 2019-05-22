using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using API.WindowsService.Controllers;
using API.WindowsService.Models;
using API.WindowsService.Test.Helpers;
using Contract;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using SemanticComparison;
using SemanticComparison.Fluent;

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class ScrubbingJobControllerTest
    {
        private IFixture fixture;
        private Mock<IHelper> _helperMock;

        private readonly ScrubbingJobRequestModel _scrubbingJobRequestModel = new ScrubbingJobRequestModel
        {
            OutputFolder = $@"\\net\nas\odtest\Test\OD2\MediaCache\Marvin\scrubbing\{Guid.NewGuid()}", //Must have read/write access to this folder.
            SourceFilename = @"\\SomeServer\RandomShare\AndFolder\videofile.mp4",
            FirstThumbnailOffsetInSeconds = 10,
            MaxSecondsBetweenThumbnails = 10,
            SpriteSheetSizes = new List<string>{"FiveByFive", "TenByTen"},
            ThumbnailResolutions = new List<string>{"160:90", "320:180"},
            Needed = DateTimeOffset.UtcNow
        };

        private readonly Mediainfo _mediaInfo = new Mediainfo
        {
            Duration = 2539,
            Framerate = 25.0,
            Frames = 63493,
            Height = 360,
            Width = 640,
            Interlaced = false
        };

        [SetUp]
        public void Setup()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            _helperMock = fixture.Freeze<Mock<IHelper>>();
            _helperMock.Setup(x => x.GetMediainfo(It.IsAny<string>())).Returns(_mediaInfo);

            if (!Directory.Exists(_scrubbingJobRequestModel.OutputFolder))
                Directory.CreateDirectory(_scrubbingJobRequestModel.OutputFolder);
        }

        [TearDown]
        public void CleanUp()
        {
            if (Directory.Exists(_scrubbingJobRequestModel.OutputFolder))
                Directory.Delete(_scrubbingJobRequestModel.OutputFolder, true);
        }

        [Test]
        public void CreateNew_Scrubbing()
        {
            // Arrange
            var repositoryMock = fixture.Freeze<Mock<IScrubbingJobRepository>>();
            var sut = fixture.Create<ScrubbingJobController>();
            var expectedScrubbingJobRequest = CreateScrubbingJobRequestMapping();
            var expectedWebVttFiles = new List<string>();
            foreach (var spriteSheetSize in _scrubbingJobRequestModel.SpriteSheetSizes)
            {
                foreach (var res in _scrubbingJobRequestModel.ThumbnailResolutions)
                    expectedWebVttFiles.Add($"{_scrubbingJobRequestModel.OutputFolder}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(_scrubbingJobRequestModel.SourceFilename)}-{res.Replace(':','x')}-{spriteSheetSize}.vtt");
            }

            repositoryMock.Setup(x => x.Add(It.IsAny<ScrubbingJobRequest>(), It.IsAny<ICollection<ScrubbingJob>>()))
                .Callback<ScrubbingJobRequest, ICollection<ScrubbingJob>>((actualScrubbingJobRequest, actualFFmpegJobs) =>
                {
                    Assert.AreEqual(expectedScrubbingJobRequest, actualScrubbingJobRequest);
                    Assert.AreEqual(4, actualFFmpegJobs.Count);
                    foreach (var filename in expectedWebVttFiles)
                    {
                        Assert.AreEqual(true, File.Exists(filename), $"WebVTT file missing [{filename}]");
                    }
                });

            // Act
            sut.CreateNew(_scrubbingJobRequestModel);
            // Asserts are above in callback
        }


        private Likeness<ScrubbingJobRequest, ScrubbingJobRequest> CreateScrubbingJobRequestMapping()
        {
            return new Likeness<ScrubbingJobRequest, ScrubbingJobRequest>(new ScrubbingJobRequest
            {
                OutputFolder = _scrubbingJobRequestModel.OutputFolder,
                SourceFilename = _scrubbingJobRequestModel.SourceFilename,
                FirstThumbnailOffsetInSeconds = _scrubbingJobRequestModel.FirstThumbnailOffsetInSeconds,
                MaxSecondsBetweenThumbnails = _scrubbingJobRequestModel.MaxSecondsBetweenThumbnails,
                Needed = _scrubbingJobRequestModel.Needed,
                SpriteSheetSizes = _scrubbingJobRequestModel.SpriteSheetSizes.ConvertAll(x => (SpriteSheetSize) Enum.Parse(typeof(SpriteSheetSize), x)),
                ThumbnailResolutions = _scrubbingJobRequestModel.ThumbnailResolutions
            });
        }

    }
}

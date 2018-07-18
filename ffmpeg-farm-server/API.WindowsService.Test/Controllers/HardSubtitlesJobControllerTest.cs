using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.WindowsService.Controllers;
using API.WindowsService.Models;
using API.WindowsService.Test.Helpers;
using Contract;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using SemanticComparison;
using API.Service;

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class HardSubtitlesJobControllerTest
    {
        private readonly string _hardSubtitlesStyle = "Fontname=TiresiasScreenfont,Fontsize=16,PrimaryColour=&H00FFFFFF,OutlineColour=&HFF000000,BackColour=&H80000000,BorderStyle=4,Outline=0,Shadow=0,MarginL=10,MarginR=10,MarginV=10";

        private HardSubtitlesJobRequestModel _requestModel = null;
        private IFixture fixture;
        private Mock<IHardSubtitlesJobRepository> repositoryMock;

        [SetUp]
        public void Setup()
        {
            _requestModel = new HardSubtitlesJobRequestModel
            {
                DestinationFilename = "krhaHardSubsTest.mxf",
                CodecId = "dvpp",
                SubtitlesFilename = "subtitles.XS1",
                VideoSourceFilename = @"\\ondnas01\MediaCache\Highres\4127a1e750fe4b9fa0dca78c011855f6.mxf",
                OutputFolder = @"\\ondnas01\MediaCache\Test\marvin\krha",
                Inpoint = TimeSpan.Zero,
                Needed = DateTimeOffset.UtcNow
            };

            fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            repositoryMock = fixture.Freeze<Mock<IHardSubtitlesJobRepository>>();
            var helperMock = fixture.Freeze<Mock<IHelper>>();
            helperMock.Setup(x => x.HardSubtitlesStyle()).Returns(_hardSubtitlesStyle);
        }

        [Test]
        public void CreateXDCAM_Repository_Mapping()
        {
            // Arrange
            var sut = fixture.Create<HardSubtitlesJobController>();
            var expectedHardSubtitlesJobRequest = CreateHardSubtitlesJobRequestMapping();
            _requestModel.LeftStream = 1;
            _requestModel.LeftStream = 1;
            _requestModel.CodecId = "xd5c";

            var expectedFFmpegJob = CreateFFmpegJobMappingXDCAM();

            repositoryMock.Setup(x => x.Add(It.IsAny<HardSubtitlesJobRequest>(), It.IsAny<ICollection<FFmpegJob>>()))
                .Callback<HardSubtitlesJobRequest, ICollection<FFmpegJob>>((actualAudioDemuxJobRequest, actualFFmpegJobs) =>
                {
                   expectedHardSubtitlesJobRequest.ShouldEqual(actualAudioDemuxJobRequest);
                    Assert.AreEqual(1, actualFFmpegJobs.Count);
                    expectedFFmpegJob.ShouldEqual(actualFFmpegJobs.Cast<HardSubtitlesJob>().Single());
                });

            // Act
            sut.CreateNew(_requestModel);
            
            // Asserts are above in callback
        }

        private Likeness<HardSubtitlesJob, HardSubtitlesJob> CreateFFmpegJobMappingXDCAM()
        {
            var destinationFilename = $"{_requestModel.OutputFolder}{Path.DirectorySeparatorChar}{_requestModel.DestinationFilename}";
            return new Likeness<HardSubtitlesJob, HardSubtitlesJob>(new HardSubtitlesJob
            {
                DestinationFilename = destinationFilename,
                Needed = _requestModel.Needed,
                Arguments = $@"-xerror -i ""{_requestModel.VideoSourceFilename}"" -filter_complex ""subtitles='{_requestModel.SubtitlesFilename.Replace("\\", "\\\\")}':force_style='{_hardSubtitlesStyle}'"" -preset ultrafast -c:v mpeg2video -b:v 50M -map 0:1 -y ""{destinationFilename}""",
                State = TranscodingJobState.Queued,

            });
        }

        [Test]
        public void CreateDVCPRO_Repository_Mapping()
        {
            // Arrange
            var sut = fixture.Create<HardSubtitlesJobController>();
            var expectedHardSubtitlesJobRequest = CreateHardSubtitlesJobRequestMapping();

            var expectedFFmpegJob = CreateFFmpegJobMappingDVCPRO();

            repositoryMock.Setup(x => x.Add(It.IsAny<HardSubtitlesJobRequest>(), It.IsAny<ICollection<FFmpegJob>>()))
                .Callback<HardSubtitlesJobRequest, ICollection<FFmpegJob>>((actualAudioDemuxJobRequest, actualFFmpegJobs) =>
                {
                    expectedHardSubtitlesJobRequest.ShouldEqual(actualAudioDemuxJobRequest);
                    Assert.AreEqual(1, actualFFmpegJobs.Count);
                    expectedFFmpegJob.ShouldEqual(actualFFmpegJobs.Cast<HardSubtitlesJob>().Single());
                });

            // Act
            sut.CreateNew(_requestModel);

            // Asserts are above in callback
        }

        private Likeness<HardSubtitlesJob, HardSubtitlesJob> CreateFFmpegJobMappingDVCPRO()
        {
            var destinationFilename = $"{_requestModel.OutputFolder}{Path.DirectorySeparatorChar}{_requestModel.DestinationFilename}";
            return new Likeness<HardSubtitlesJob, HardSubtitlesJob>(new HardSubtitlesJob
            {
                DestinationFilename = destinationFilename,
                Needed = _requestModel.Needed,
                Arguments = $@"-xerror -i ""{_requestModel.VideoSourceFilename}"" -filter_complex ""subtitles='{GetEscapedFileName(_requestModel.SubtitlesFilename)}':force_style='{_hardSubtitlesStyle}'"" -preset ultrafast -c:v mpeg4 -b:v 50M -map 0:1 -y ""{destinationFilename}""",
                State = TranscodingJobState.Queued,

            });
        }

        [Test]
        public void CreateMappedStereo_Repository_Mapping()
        {
            // Arrange
            var sut = fixture.Create<HardSubtitlesJobController>();
            var expectedHardSubtitlesJobRequest = CreateHardSubtitlesJobRequestMapping();
            _requestModel.LeftStream = 1;
            _requestModel.RightStream = 2;

            var expectedFFmpegJob = CreateFFmpegJobMappingMappedStereo();

            repositoryMock.Setup(x => x.Add(It.IsAny<HardSubtitlesJobRequest>(), It.IsAny<ICollection<FFmpegJob>>()))
                .Callback<HardSubtitlesJobRequest, ICollection<FFmpegJob>>((actualAudioDemuxJobRequest, actualFFmpegJobs) =>
                {
                    expectedHardSubtitlesJobRequest.ShouldEqual(actualAudioDemuxJobRequest);
                    Assert.AreEqual(1, actualFFmpegJobs.Count);
                    expectedFFmpegJob.ShouldEqual(actualFFmpegJobs.Cast<HardSubtitlesJob>().Single());
                });

            // Act
            sut.CreateNew(_requestModel);

            // Asserts are above in callback
        }

        private Likeness<HardSubtitlesJob, HardSubtitlesJob> CreateFFmpegJobMappingMappedStereo()
        {
            var destinationFilename = $"{_requestModel.OutputFolder}{Path.DirectorySeparatorChar}{_requestModel.DestinationFilename}";
            return new Likeness<HardSubtitlesJob, HardSubtitlesJob>(new HardSubtitlesJob
            {
                DestinationFilename = destinationFilename,
                Needed = _requestModel.Needed,
                Arguments = $@"-xerror -i ""{_requestModel.VideoSourceFilename}"" -filter_complex ""subtitles='{GetEscapedFileName(_requestModel.SubtitlesFilename)}':force_style='{_hardSubtitlesStyle}';[0:1][0:2]amerge=inputs=2[aout]"" -preset ultrafast -c:v mpeg4 -b:v 50M -map ""[aout]"" -y ""{destinationFilename}""",
                State = TranscodingJobState.Queued,
            });
        }

        [Test]
        public void CreateInpoint_Repository_Mapping()
        {
            // Arrange
            var sut = fixture.Create<HardSubtitlesJobController>();
            var expectedHardSubtitlesJobRequest = CreateHardSubtitlesJobRequestMapping();
            _requestModel.LeftStream = 1;
            _requestModel.RightStream = 2;
            _requestModel.Inpoint = TimeSpan.FromSeconds(30);

            var expectedFFmpegJob = CreateFFmpegJobMappingWithInpoint();

            repositoryMock.Setup(x => x.Add(It.IsAny<HardSubtitlesJobRequest>(), It.IsAny<ICollection<FFmpegJob>>()))
                .Callback<HardSubtitlesJobRequest, ICollection<FFmpegJob>>((actualAudioDemuxJobRequest, actualFFmpegJobs) =>
                {
                    expectedHardSubtitlesJobRequest.ShouldEqual(actualAudioDemuxJobRequest);
                    Assert.AreEqual(1, actualFFmpegJobs.Count);
                    expectedFFmpegJob.ShouldEqual(actualFFmpegJobs.Cast<HardSubtitlesJob>().Single());
                });

            // Act
            sut.CreateNew(_requestModel);

            // Asserts are above in callback
        }

        private Likeness<HardSubtitlesJob, HardSubtitlesJob> CreateFFmpegJobMappingWithInpoint()
        {
            var destinationFilename = $"{_requestModel.OutputFolder}{Path.DirectorySeparatorChar}{_requestModel.DestinationFilename}";
            return new Likeness<HardSubtitlesJob, HardSubtitlesJob>(new HardSubtitlesJob
            {
                DestinationFilename = destinationFilename,
                Needed = _requestModel.Needed,
                Arguments = $@"-ss 0:00:30 -xerror -i ""{_requestModel.VideoSourceFilename}"" -filter_complex ""subtitles='{GetEscapedFileName(_requestModel.SubtitlesFilename)}':force_style='{_hardSubtitlesStyle}';[0:1][0:2]amerge=inputs=2[aout]"" -preset ultrafast -c:v mpeg4 -b:v 50M -map ""[aout]"" -y ""{destinationFilename}""",
                State = TranscodingJobState.Queued,

            });
        }

        private Likeness<HardSubtitlesJobRequest, HardSubtitlesJobRequest> CreateHardSubtitlesJobRequestMapping()
        {
            return new Likeness<HardSubtitlesJobRequest, HardSubtitlesJobRequest>(new HardSubtitlesJobRequest
            {
                OutputFolder = _requestModel.OutputFolder,
                SubtitlesFilename = _requestModel.SubtitlesFilename,
                DestinationFilename = _requestModel.DestinationFilename,
                VideoSourceFilename = _requestModel.VideoSourceFilename,
            });
        }

        private string GetEscapedFileName(string fileName)
        {
            return fileName.Replace("\\", "\\\\");
        }
    }
}

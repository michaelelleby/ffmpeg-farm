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

namespace API.WindowsService.Test.Controllers
{
    [TestFixture]
    public class AudioDemuxJobControllerTest
    {
        private readonly AudioDemuxJobRequestModel _audioDemuxJobRequestModel = new AudioDemuxJobRequestModel
        {
            DestinationFilename = "krhaDemuxTest.wav",
            VideoSourceFilename = @"\\ondnas01\MediaCache\Highres\4127a1e750fe4b9fa0dca78c011855f6.mov",
            OutputFolder = @"\\ondnas01\MediaCache\Test\marvin\krha",
            Inpoint = TimeSpan.Zero,
            Needed = DateTimeOffset.UtcNow
        };
        
        [Test]
        public void CreateNew_Repository_Mapping()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repositoryMock = fixture.Freeze<Mock<IAudioDemuxJobRepository>>();
            var sut = fixture.Create<AudioDemuxJobController>();
            var expectedAudioDemuxJobRequest = CreateAudioDemuxJobRequestMapping();

            var expectedFFmpegJob = CreateFFmpegJobMapping();

            repositoryMock.Setup(x => x.Add(It.IsAny<AudioDemuxJobRequest>(), It.IsAny<ICollection<FFmpegJob>>()))
                .Callback<AudioDemuxJobRequest, ICollection<FFmpegJob>>((actualAudioDemuxJobRequest, actualFFmpegJobs) =>
                {
                   expectedAudioDemuxJobRequest.ShouldEqual(actualAudioDemuxJobRequest);
                    Assert.AreEqual(1, actualFFmpegJobs.Count);
                    expectedFFmpegJob.ShouldEqual(actualFFmpegJobs.Cast<AudioDemuxJob>().Single());
                });

            // Act
            sut.CreateNew(_audioDemuxJobRequestModel);
            
            // Asserts are above in callback
        }

        private Likeness<AudioDemuxJobRequest, AudioDemuxJobRequest> CreateAudioDemuxJobRequestMapping()
        {
            return new Likeness<AudioDemuxJobRequest, AudioDemuxJobRequest>(new AudioDemuxJobRequest
            {
                OutputFolder = _audioDemuxJobRequestModel.OutputFolder,
                DestinationFilename = _audioDemuxJobRequestModel.DestinationFilename,
                VideoSourceFilename = _audioDemuxJobRequestModel.VideoSourceFilename,
            });
        }

        private Likeness<AudioDemuxJob, AudioDemuxJob> CreateFFmpegJobMapping()
        {
            var destinationFilename = $"{_audioDemuxJobRequestModel.OutputFolder}{Path.DirectorySeparatorChar}{_audioDemuxJobRequestModel.DestinationFilename}";
            return new Likeness<AudioDemuxJob, AudioDemuxJob>(new AudioDemuxJob
            {
                DestinationFilename = destinationFilename,
                Needed = _audioDemuxJobRequestModel.Needed.LocalDateTime,
                Arguments = $"-i {_audioDemuxJobRequestModel.VideoSourceFilename} {destinationFilename} -y",
                State = TranscodingJobState.Queued,
                
            });
        }
    }
}

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
            _audioDemuxJobRequestModel.LeftStream = 1; //Stream 1 is a stereo stream
            _audioDemuxJobRequestModel.RightStream = _audioDemuxJobRequestModel.LeftStream; //Same stream as left since stream 1 is stereo

            var expectedFFmpegJob = CreateFFmpegJobMappingSingleStereoStream();

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

        [Test]
        public void CreateMultiChannel_Repository_Mapping()
        {
            // Arrange
            var fixture = new Fixture().Customize(new AutoMoqCustomization()).Customize(new ApiControllerConventions());
            var repositoryMock = fixture.Freeze<Mock<IAudioDemuxJobRepository>>();
            var sut = fixture.Create<AudioDemuxJobController>();
            var expectedAudioDemuxJobRequest = CreateAudioDemuxJobRequestMapping();
            _audioDemuxJobRequestModel.LeftStream = 1; //Stream 1 is a mono stream
            _audioDemuxJobRequestModel.RightStream = 2; //Stream 2 is a mono stream

            var expectedFFmpegJob = CreateFFmpegJobMappingMultiStream();

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

        private Likeness<AudioDemuxJob, AudioDemuxJob> CreateFFmpegJobMappingSingleStereoStream()
        {
            var destinationFilename = $"{_audioDemuxJobRequestModel.OutputFolder}{Path.DirectorySeparatorChar}{_audioDemuxJobRequestModel.DestinationFilename}";
            return new Likeness<AudioDemuxJob, AudioDemuxJob>(new AudioDemuxJob
            {
                DestinationFilename = destinationFilename,
                Needed = _audioDemuxJobRequestModel.Needed.LocalDateTime,
                Arguments = $"-i {_audioDemuxJobRequestModel.VideoSourceFilename} -map 0:{_audioDemuxJobRequestModel.LeftStream} {destinationFilename} -y",
                State = TranscodingJobState.Queued,
                
            });
        }

        private Likeness<AudioDemuxJob, AudioDemuxJob> CreateFFmpegJobMappingMultiStream()
        {
            var destinationFilename = $"{_audioDemuxJobRequestModel.OutputFolder}{Path.DirectorySeparatorChar}{_audioDemuxJobRequestModel.DestinationFilename}";
            return new Likeness<AudioDemuxJob, AudioDemuxJob>(new AudioDemuxJob
            {
                DestinationFilename = destinationFilename,
                Needed = _audioDemuxJobRequestModel.Needed.LocalDateTime,
                Arguments = $"-i {_audioDemuxJobRequestModel.VideoSourceFilename} -filter_complex \"[0:{_audioDemuxJobRequestModel.LeftStream}][0:{_audioDemuxJobRequestModel.RightStream}]amerge = inputs = 2[aout]\" -map \"[aout]\" {destinationFilename} -y",
                State = TranscodingJobState.Queued,

            });
        }
    }
}

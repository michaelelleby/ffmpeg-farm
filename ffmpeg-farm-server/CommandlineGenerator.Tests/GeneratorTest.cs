using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using Moq;
using NUnit.Framework;

namespace CommandlineGenerator.Tests
{
    [TestFixture]
    public class GeneratorTest
    {
        [Test]
        public void GenerateAudioCommandlineShouldIncludeForceIfOverwriteOutputIsEnabledInSettings()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            AudioDestinationFormat target = new AudioDestinationFormat
            {
                AudioCodec = Codec.AAC,
                Format = ContainerFormat.MP4,
                Bitrate = 192,
                Channels = Channels.Stereo
            };
            var sourceFilenames = new List<string> {"testfilename.wav"};
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.OverwriteOutput)
                .Returns(true);

            // Act
            string result = sut.GenerateAudioCommandline(target, sourceFilenames, @"c:\somepath\unittest.mp4");

            // Assert
            const string expectedCommandline = @"-y -i ""testfilename.wav"" -c:a aac -b:a 192k -vn -movflags +faststart -map_metadata -1 -f mp4 ""c:\somepath\unittest.mp4""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }

        [Test]
        public void GenerateAudioCommandlineShouldIncludeXerrorIfAbortOnErrorIsEnabledInSettings()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            AudioDestinationFormat target = new AudioDestinationFormat
            {
                AudioCodec = Codec.AAC,
                Format = ContainerFormat.MP4,
                Bitrate = 192,
                Channels = Channels.Stereo
            };
            var sourceFilenames = new List<string> { "testfilename.wav" };
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.AbortOnError)
                .Returns(true);

            // Act
            string result = sut.GenerateAudioCommandline(target, sourceFilenames, @"c:\somepath\unittest.mp4");

            // Assert
            const string expectedCommandline = @"-xerror -i ""testfilename.wav"" -c:a aac -b:a 192k -vn -movflags +faststart -map_metadata -1 -f mp4 ""c:\somepath\unittest.mp4""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }

        [Test]
        public void GenerateAudioCommandlineShouldIncludeMappingIfMultipleSourceFiles()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            AudioDestinationFormat target = new AudioDestinationFormat
            {
                AudioCodec = Codec.AAC,
                Format = ContainerFormat.MP4,
                Bitrate = 192,
                Channels = Channels.Stereo
            };
            var sourceFilenames = new List<string> { "test.wav", "test2.wav", "test3.wav" };
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.AbortOnError)
                .Returns(true);

            // Act
            string result = sut.GenerateAudioCommandline(target, sourceFilenames, @"c:\somepath\unittest.mp4");

            // Assert
            const string expectedCommandline = @"-xerror -i ""test.wav"" -i ""test2.wav"" -i ""test3.wav"" -filter_complex [0:0][1:0][2:0]concat=n=3:a=1:v=0 -c:a aac -b:a 192k -vn -movflags +faststart -map_metadata -1 -f mp4 ""c:\somepath\unittest.mp4""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }

        [Test]
        public void GenerateMuxCommandlineShouldIncludeForceIfOverwriteOutputIsEnabledInSettings()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            AudioDestinationFormat target = new AudioDestinationFormat
            {
                AudioCodec = Codec.AAC,
                Format = ContainerFormat.MP4,
                Bitrate = 192,
                Channels = Channels.Stereo
            };
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.OverwriteOutput)
                .Returns(true);

            // Act
            string result = sut.GenerateMuxCommandline("source.mov", "source.wav", "output.mov", TimeSpan.Zero);

            // Assert
            const string expectedCommandline = @"-y -i ""source.mov"" -i ""source.wav"" -map 0:v:0 -map 1:a:0 -c copy ""output.mov""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }
        
        [Test]
        public void GenerateDemuxCommandlineShouldIncludeXerrorAbortOnErrorIsEnabledInSettings()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.AbortOnError)
                .Returns(true);

            // Act
            string result = sut.GenerateMuxCommandline("source.mov", "source.wav", "output.mov", TimeSpan.Zero);
            // Assert
            const string expectedCommandline = @"-xerror -i ""source.mov"" -i ""source.wav"" -map 0:v:0 -map 1:a:0 -c copy ""output.mov""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }

        [Test]
        public void GenerateDemuxCommandlineShouldIncludeSeekingIfInpointIsSet()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.AbortOnError)
                .Returns(true);
            settings.Setup(m => m.OverwriteOutput)
                .Returns(true);

            // Act
            string result = sut.GenerateMuxCommandline("source.mov", "source.wav", "output.mov", TimeSpan.FromMinutes(5));
            // Assert
            const string expectedCommandline = @"-y -xerror -ss 00:05:00 -i ""source.mov"" -i ""source.wav"" -map 0:v:0 -map 1:a:0 -c copy ""output.mov""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }

        [Test]
        public void GenerateDemuxCommandlineShouldGenerateCorrect()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            var settings = fixture.Freeze<Mock<IApiSettings>>();
            IGenerator sut = fixture.Create<Generator>();

            settings.Setup(m => m.AbortOnError)
                .Returns(true);
            settings.Setup(m => m.OverwriteOutput)
                .Returns(true);

            // Act
            string result = sut.GenerateMuxCommandline("source.mov", "source.wav", "output.mov", TimeSpan.Zero);
            // Assert
            const string expectedCommandline = @"-y -xerror -i ""source.mov"" -i ""source.wav"" -map 0:v:0 -map 1:a:0 -c copy ""output.mov""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }
    }
}

using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Contract;
using NUnit.Framework;

namespace CommandlineGenerator.Tests
{
    [TestFixture]
    public class GeneratorTest
    {
        [Test]
        public void ShouldSetForceIfOverwriteOutputIsEnabledInSettings()
        {
            // Arrange
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization());
            AudioDestinationFormat target = new AudioDestinationFormat
            {
                AudioCodec = Codec.AAC,
                Format = ContainerFormat.MP4,
                Bitrate = 192,
                Channels = Channels.Stereo
            };
            var sourceFilenames = new List<string> {"testfilename.wav"};
            IGenerator sut = fixture.Create<Generator>();

            // Act
            string result = sut.GenerateAudioCommandline(target, sourceFilenames, @"c:\somepath\unittest.mp4");

            // Assert
            string expectedCommandline = @"-i ""testfilename.wav"" -c:a aac -b:a 192k -vn -movflags +faststart -map_metadata -1 -f mp4 ""c:\somepath\unittest.mp4""";
            Assert.That(result, Is.EqualTo(expectedCommandline));
        }
    }
}

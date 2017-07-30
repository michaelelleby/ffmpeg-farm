using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using Contract;
using NUnit.Framework;

namespace API.Services.FFmpeg.Tests
{
    [TestFixture]
    public class CommandlineGeneratorTest
    {
        [Test]
        public void Get_ShouldStartWithInput()
        {
            // Arrange
            const string inputFilename = "test input filename";
            var parameters = new FFmpegParameters
            {
                Inputfile = inputFilename
            };

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            StringAssert.StartsWith($@"-i ""{inputFilename}", result);
        }

        [Test]
        public void Get_ShouldSetAudioCodecAndBitrate()
        {
            // Arrange
            const string inputFilename = "test input filename";
            var parameters = GetTestParameters(inputFilename, audioCodec: AudioCodec.AAC, audioBitrate: 131072);

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            string expected = $@"-i ""{inputFilename}"" -codec:a aac -b:a 128k";

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Get_ShouldSetDeinterlaceFilter()
        {
            // Arrange
            const string inputFilename = "test input filename";
            var parameters = GetTestParameters(inputFilename,
                FFmpegParameters.DeinterlaceSettings.DeinterlaceMode.SendFrame,
                FFmpegParameters.DeinterlaceSettings.DeinterlaceParity.Auto, true);

            parameters.VideoParam = null;
            parameters.AudioParam = new FFmpegParameters.Audio
            {
                Codec = AudioCodec.AAC,
                Bitrate = 131072
            };
            parameters.Deinterlace = new FFmpegParameters.DeinterlaceSettings
            {
                Mode = FFmpegParameters.DeinterlaceSettings.DeinterlaceMode.SendFrame,
                Parity = FFmpegParameters.DeinterlaceSettings.DeinterlaceParity.Auto,
                DeinterlaceAllFrames = true
            };

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            string expected = $@"-i ""{inputFilename}"" -filter_complex ""yadif=0:-1:0"" -codec:a aac -b:a 128k";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Get_ShouldSetVideoCodec()
        {
            // Arrange
            const string inputFilename = "test input filename";
            var parameters = GetTestParameters(inputFilename, videoBitrate: 1024000, videoCodec: VideoCodec.LibX264, videoPreset: "medium");

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            string expected = $@"-i ""{inputFilename}"" -codec:v libx264 -preset medium -b:v 1000k";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        [TestCase(1024, 480)]
        [TestCase(1024, 576)]
        [TestCase(720, 576)]
        [TestCase(720, 480)]
        [TestCase(640, 360)]
        public void Get_ShouldSetResizeInfo(int width, int height)
        {
            // Arrange
            const string inputFilename = "test input filename";

            var parameters = GetTestParameters(inputFilename, videoCodec: VideoCodec.LibX264, videoBitrate: 1024000,
                videoSize: new VideoSize(width, height));

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            Assert.That(result, Is.EqualTo($@"-i ""{inputFilename}"" -filter_complex ""scale={width}:{height}"" -codec:v libx264 -preset medium -b:v 1000k"));
        }

        [Test]
        public void Get_ShouldMap()
        {
            // Arrange
            const string inputFilename = "test input filename";

            var parameters = GetTestParameters(inputFilename);
            parameters.VideoParam = new FFmpegParameters.Video
            {
                VideoTarget = new List<VideoTarget>
                {
                    new VideoTarget
                    {
                        Size = new VideoSize
                        {
                            Width = 1920,
                            Height = 1080
                        },
                        Preset = "medium",
                        Bitrate = 3072000
                    },
                    new VideoTarget
                    {
                        Size = new VideoSize
                        {
                            Width = 1920,
                            Height = 1080
                        },
                        Preset = "medium",
                        Bitrate = 2560000
                    }
                }
            };

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            string expected = $@"-i ""{inputFilename}"" -filter_complex ""scale=1920:1080,split=2[out0][out1]"" -map [out0] -codec:v libx264 -preset medium -b:v 3000k -map [out1] -codec:v libx264 -preset medium -b:v 2500k";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Get_ShouldSetBothAudioAndVideoInfo()
        {
            // Arrange
            const string inputFilename = "test input filename";
            var parameters = GetTestParameters(inputFilename,
                FFmpegParameters.DeinterlaceSettings.DeinterlaceMode.SendFrame,
                FFmpegParameters.DeinterlaceSettings.DeinterlaceParity.Auto, true, 1024000, VideoCodec.LibX264,
                "medium", AudioCodec.AAC, 131072, new VideoSize(1920, 1080));

            // Act
            var result = CommandlineGenerator.Get(parameters);

            // Assert
            string expected = $@"-i ""{inputFilename}"" -filter_complex ""yadif=0:-1:0,scale=1920:1080"" -codec:v libx264 -preset medium -b:v 1000k -codec:a aac -b:a 128k";
            Assert.That(result, Is.EqualTo(expected));
        }

        private static FFmpegParameters GetTestParameters(string inputFilename,
            FFmpegParameters.DeinterlaceSettings.DeinterlaceMode deinterlaceMode =
                FFmpegParameters.DeinterlaceSettings.DeinterlaceMode.Unknown,
            FFmpegParameters.DeinterlaceSettings.DeinterlaceParity deinterlaceParity =
                FFmpegParameters.DeinterlaceSettings.DeinterlaceParity.Unknown, bool deinterlaceAllFrames = false,
            int videoBitrate = 0, VideoCodec videoCodec = VideoCodec.Unknown, string videoPreset = "",
            AudioCodec audioCodec = AudioCodec.Unknown, int audioBitrate = 0, VideoSize videoSize = null)
        {
            if (string.IsNullOrWhiteSpace(inputFilename)) throw new ArgumentNullException(nameof(inputFilename));

            var parameters = new FFmpegParameters
            {
                Inputfile = inputFilename
            };

            if (videoCodec != VideoCodec.Unknown)
            {
                if (parameters.VideoParam == null)
                {
                    parameters.VideoParam = new FFmpegParameters.Video();
                }

                parameters.VideoParam.Codec = videoCodec;
            }
            if (videoBitrate > 0)
            {
                if (parameters.VideoParam == null)
                {
                    parameters.VideoParam = new FFmpegParameters.Video();
                }

                VideoTarget target = parameters.VideoParam.VideoTarget.FirstOrDefault();
                if (target == null)
                {
                    target = new VideoTarget();
                    parameters.VideoParam.VideoTarget.Add(target);
                }


                target.Bitrate = videoBitrate;
            }

            if (audioCodec != AudioCodec.Unknown)
            {
                if (parameters.AudioParam == null)
                {
                    parameters.AudioParam = new FFmpegParameters.Audio();
                }

                parameters.AudioParam.Codec = audioCodec;
            }

            if (audioBitrate > 0)
            {
                if (parameters.AudioParam == null)
                {
                    parameters.AudioParam = new FFmpegParameters.Audio();
                }

                parameters.AudioParam.Bitrate = audioBitrate;
            }

            if (deinterlaceMode != FFmpegParameters.DeinterlaceSettings.DeinterlaceMode.Unknown && deinterlaceParity !=
                FFmpegParameters.DeinterlaceSettings.DeinterlaceParity.Unknown)
            {
                parameters.Deinterlace = new FFmpegParameters.DeinterlaceSettings
                {
                    Mode = deinterlaceMode,
                    Parity = deinterlaceParity,
                    DeinterlaceAllFrames = deinterlaceAllFrames
                };
            }

            if (videoSize != null)
            {
                VideoTarget target = parameters.VideoParam.VideoTarget.First();
                if (target == null)
                {
                    target = new VideoTarget();
                    parameters.VideoParam.VideoTarget.Add(target);
                }

                target.Size = videoSize;
            }

            if (!string.IsNullOrWhiteSpace(videoPreset))
            {
                VideoTarget target = parameters.VideoParam.VideoTarget.First();
                if (target == null)
                {
                    target = new VideoTarget();
                    parameters.VideoParam.VideoTarget.Add(target);
                }

                target.Preset = videoPreset;
            }

            return parameters;
        }
    }
}
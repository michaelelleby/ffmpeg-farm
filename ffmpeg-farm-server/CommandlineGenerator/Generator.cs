using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contract;

namespace CommandlineGenerator
{
    public class Generator : IGenerator
    {
        private readonly IHelper _helper;
        private readonly IApiSettings _settings;

        public Generator(IHelper helper, IApiSettings settings)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string GenerateAudioCommandline(AudioDestinationFormat target, IReadOnlyCollection<string> sourceFilenames, string fullpath)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (sourceFilenames == null) throw new ArgumentNullException(nameof(sourceFilenames));
            if (sourceFilenames.Any() == false) throw new ArgumentOutOfRangeException(nameof(sourceFilenames), "At least one source filename must be specified");
            
            ICollection<string> commandline = new List<string>();

            if (_settings.OverwriteOutput)
                commandline.Add("-y");
            if (_settings.AbortOnError)
                commandline.Add("-xerror");

            foreach (string filename in sourceFilenames)
            {
                commandline.Add($@"-i ""{filename}""");
            }

            if (sourceFilenames.Count > 1)
            {
                /*RESULT:
                 * -y -xerror
                 * -i "\\ondnas01\MediaCache\Test\test.mp3" -i "\\ondnas01\MediaCache\Test\radioavis.mp3" -i "\\ondnas01\MediaCache\Test\temp.mp3"
                 * -filter_complex
                 * [0:0][1:0][2:0]concat=n=3:a=1:v=0
                 * -c:a mp3 -b:a 64k -vn -map_metadata -1 -f MP3 \\ondnas01\MediaCache\Test\marvin\ffmpeg\test2.mp3
                */
                var streams = new StringBuilder();
                int streamCount;

                for (streamCount = 0; streamCount < sourceFilenames.Count; streamCount++)
                {
                    streams.AppendFormat("[{0}:0]", streamCount);
                }

                commandline.Add($"-filter_complex {streams}concat=n={streamCount}:a=1:v=0");
            }

            commandline.Add($"-c:a {target.AudioCodec.ToString().ToLower()}");
            commandline.Add($"-b:a {target.Bitrate}k");
            commandline.Add("-vn");

            if (target.Format == ContainerFormat.MP4)
                commandline.Add("-movflags +faststart");

            commandline.Add("-map_metadata -1");
            commandline.Add($"-f {target.Format.ToString().ToLower()}");
            commandline.Add($@"""{fullpath}""");

            return string.Join(" ", commandline);
        }

        public string GenerateMuxCommandline(string inputVideoFilename, string inputAudioFilename,
            string outputFilename, TimeSpan inpoint)
        {
            ICollection<string> commandline = new List<string>();

            if (_settings.OverwriteOutput)
                commandline.Add("-y");
            if (_settings.AbortOnError)
                commandline.Add("-xerror");

            if (inpoint > TimeSpan.Zero)
            {
                commandline.Add($"-ss {inpoint:hh\\:mm\\:ss}");
            }

            commandline.Add($@"-i ""{inputVideoFilename}""");
            commandline.Add($@"-i ""{inputAudioFilename}""");
            commandline.Add("-map 0:v:0");
            commandline.Add("-map 1:a:0");
            commandline.Add("-c copy");
            commandline.Add($@"""{outputFilename}""");

            return string.Join(" ", commandline);
        }
    }
}

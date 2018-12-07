using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FFmpegFarm.Worker
{
    public class LogFileWriter : IDisposable
    {
        private readonly StreamWriter _streamWriter;
        private readonly StringBuilder _stringBuilder;

        public LogFileWriter(string logFileFullPath)
        {
            _streamWriter = new StreamWriter(logFileFullPath);
            _stringBuilder = new StringBuilder();
            _streamWriter.AutoFlush = true;
        }
        public void Dispose()
        {
            _streamWriter.Dispose();
        }

        public void WriteLine(string text)
        {
            _streamWriter.WriteLine(text);
            _stringBuilder.AppendLine(text);
        }

        public bool FfmpegDetectedError()
        {
            // FFmpeg will return exit code 0 even when writing to the output the following:
            // Error while decoding stream #0:0: Invalid data found when processing input
            // so we need to check if there is a line beginning with the word Error
            string output = _stringBuilder.ToString();
            bool ffmpegError =
                Regex.IsMatch(output, @"\] Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
                || Regex.IsMatch(output, @"^Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline);

            if (ffmpegError && IsInvalidPcmPacketError(output))
                return false; //The output file is fine and the subsequent validation passes, so we ignore this error

            return ffmpegError;
        }

        private bool IsInvalidPcmPacketError(string output)
        {
            //This can happen for Dalet files on the last packet, the ffmpeg output looks like this:
            //  size = 339076kB time = 00:30:08.40 bitrate = 1536.0kbits / s speed = 8.12x
            //  size = 339711kB time = 00:30:11.78 bitrate = 1536.0kbits / s speed = 7.89x
            //  video:0kB audio:339710kB subtitle:0kB other streams: 0kB global headers: 0kB muxing overhead: 0.000053 %
            //  [pcm_s16le @ 00000000008d8220] Multiple frames in a packet.
            //  [pcm_s16le @ 00000000008d8220] Invalid PCM packet, data has size 2 but at least a size of 4 was expected
            //  Error while decoding stream #0:0: Invalid data found when processing input
            //  size=  339710kB time = 00:30:11.78 bitrate=1536.0kbits/s speed = 8.12x
            //  video:0kB audio:339710kB subtitle:0kB other streams:0kB global headers:0kB muxing overhead: 0.000053%

            return output.Contains("Invalid PCM packet, data has size 2 but at least a size of 4 was expected");
        }

        public string GetOutput()
        {
            return _stringBuilder.ToString();
        }
    }
}

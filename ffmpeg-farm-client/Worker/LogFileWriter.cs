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

            return Regex.IsMatch(_stringBuilder.ToString(), @"\] Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
                || Regex.IsMatch(_stringBuilder.ToString(), @"^Error",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public string GetOutput()
        {
            return _stringBuilder.ToString();
        }
    }
}

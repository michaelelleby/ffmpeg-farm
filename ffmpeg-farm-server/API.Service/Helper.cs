using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Contract;

namespace API.Service
{
    public class Helper : IHelper
    {
        public IDbConnection GetConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);
        }

        public Mediainfo GetMediainfo(string sourceFilename)
        {
            if (string.IsNullOrWhiteSpace(sourceFilename)) throw new ArgumentNullException("sourceFilename");
            if (!File.Exists(sourceFilename))
                throw new FileNotFoundException("Media not found when trying to get file duration", sourceFilename);

            string mediaInfoPath = ConfigurationManager.AppSettings["MediaInfoPath"];
            if (!File.Exists(mediaInfoPath))
                throw new FileNotFoundException("MediaInfo.exe was not found", mediaInfoPath);

            var mediaInfoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    Arguments =
                        $@"--inform=Video;%Duration%|%FrameRate%|%Width%|%Height%|%ScanType%|%FrameCount% ""{sourceFilename}""",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            string[] output = mediaInfoProcess.StandardOutput.ReadToEnd().Split('|');

            return new Mediainfo
            {
                Duration = Convert.ToInt32(output[0]) / 1000, // Duration is reported in milliseconds
                Framerate = float.Parse(output[1], NumberFormatInfo.InvariantInfo),
                Width = Convert.ToInt32(output[2]),
                Height = Convert.ToInt32(output[3]),
                Interlaced = output[4].ToUpperInvariant() == "INTERLACED",
                Frames = Convert.ToInt32(output[5])
            };
        }

        public int GetDuration(string sourceFilename)
        {
            if (string.IsNullOrWhiteSpace(sourceFilename)) throw new ArgumentNullException("sourceFilename");
            if (!File.Exists(sourceFilename))
                throw new FileNotFoundException("Media not found when trying to get file duration", sourceFilename);

            string mediaInfoPath = ConfigurationManager.AppSettings["MediaInfoPath"];
            if (!File.Exists(mediaInfoPath))
                throw new FileNotFoundException("MediaInfo.exe was not found", mediaInfoPath);

            var mediaInfoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    Arguments = $@"--inform=General;%Duration% ""{sourceFilename}""",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            string value = mediaInfoProcess.StandardOutput.ReadToEnd();

            int result = 0;
            int.TryParse(value, out result); //Dvcpro files without mov wrapping have no duration

            return result / 1000;
        }

        public string HardSubtitlesStyle()
        {
            var cfgStyle = ConfigurationManager.AppSettings["SubtitlesStyle"];
            return string.IsNullOrWhiteSpace(cfgStyle) ? "Fontname=TiresiasScreenfont,Fontsize=16,PrimaryColour=&H00FFFFFF,OutlineColour=&HFF000000,BackColour=&H80000000,BorderStyle=4,Outline=0,Shadow=0,MarginL=10,MarginR=10,MarginV=10" : cfgStyle;
        }
    }
}
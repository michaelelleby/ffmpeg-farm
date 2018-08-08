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
                        $@"--inform=Video;%Duration%|%FrameRate%|%Width%|%Height%|%ScanType%|%FrameCount%|%Format_Commercial% ""{sourceFilename}""",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            string[] output = mediaInfoProcess.StandardOutput.ReadToEnd().Split('|');

            // Since MediaInfo can't read duration from DVCPro files, we need to calculate it manually.
            var calculatedDuration = 0;
            var calculatedTotalFrames = 0;
            var manualCalcDuration = calculateDvcProDuration(output[6], float.Parse(output[1], NumberFormatInfo.InvariantInfo), sourceFilename, ref calculatedDuration, ref calculatedTotalFrames);

            return new Mediainfo
            {
                Duration = manualCalcDuration ? calculatedDuration : Convert.ToInt32(output[0]) / 1000, // Duration is reported in milliseconds
                Framerate = float.Parse(output[1], NumberFormatInfo.InvariantInfo),
                Width = Convert.ToInt32(output[2]),
                Height = Convert.ToInt32(output[3]),
                Interlaced = output[4].ToUpperInvariant() == "INTERLACED",
                Frames = manualCalcDuration ? calculatedTotalFrames : Convert.ToInt32(output[5]),
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
            if (!int.TryParse(value, out result)) //Dvcpro files without mov wrapping have no duration
                result = GetMediainfo(sourceFilename).Duration;
            else
                result = result / 1000;

            return result;
        }

        public string HardSubtitlesStyle()
        {
            var cfgStyle = ConfigurationManager.AppSettings["SubtitlesStyle"];
            return string.IsNullOrWhiteSpace(cfgStyle) ? "Fontname=TiresiasScreenfont,Fontsize=16,PrimaryColour=&H00FFFFFF,OutlineColour=&HFF000000,BackColour=&H80000000,BorderStyle=4,Outline=0,Shadow=0,MarginL=10,MarginR=10,MarginV=10" : cfgStyle;
        }

        private bool calculateDvcProDuration(string formatCommercial, double frameRate, string sourceFilename, ref int calculatedDuration, ref int calculatedTotalFrames)
        {
            var manualCalcDuration = false;
            if (!string.IsNullOrEmpty(formatCommercial))
            {
                var formatName = formatCommercial.ToUpperInvariant();
                if (formatName.Contains("DVCPRO"))
                {
                    manualCalcDuration = true;

                    var bytesPerFrame = 144000; // DVCPro 25
                    if (formatName.Contains("DVCPRO 50"))
                        bytesPerFrame = bytesPerFrame * 2; // DVCPro 50 = 288000
                    else if (formatName.Contains("DVCPRO 100"))
                        bytesPerFrame = bytesPerFrame * 4; // DVCPro HD/100 = 576000

                    var fileSize = new FileInfo(sourceFilename).Length;
                    calculatedTotalFrames = (int)(fileSize / bytesPerFrame);
                    calculatedDuration = (int)(calculatedTotalFrames / frameRate);
                }
            }

            return manualCalcDuration;
        }
    }
}
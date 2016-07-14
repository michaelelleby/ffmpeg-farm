using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Transactions;
using System.Xml;
using Dapper;

namespace API.Service
{
    public class Helper
    {
        public static IDbConnection GetConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["mssql"].ConnectionString);
        }

        public static Mediainfo GetMediainfo(string sourceFilename)
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
                    Arguments = $@"--output=XML ""{sourceFilename}""",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            XmlDocument data = new XmlDocument();
            data.LoadXml(mediaInfoProcess.StandardOutput.ReadToEnd());

            string duration = data.SelectSingleNode(@"/MediaInfo/File/Track[@type=""General""]/Duration[matches(., ""^\d.+?"")]")?.Value;
            string framerate = data.SelectSingleNode(@"/MediaInfo/File/Track[@type=""General""]/Frame_rate[matches(., ""^[\d\.]+$"")]")?.Value;
            bool interlaced = data.SelectSingleNode(@"/MediaInfo/File/Track[@type=""Video""]/Scan_Type")?.Value == "Interlaced";

            return new Mediainfo
            {
                Duration = Convert.ToInt32(duration),
                Framerate = Convert.ToDouble(framerate),
                Interlaced = interlaced
            };
        }

        public static int GetDuration(string sourceFilename)
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

            return Convert.ToInt32(mediaInfoProcess.StandardOutput.ReadToEnd())/1000;
        }

        public static double GetFramerate(string sourceFilename)
        {
            if (String.IsNullOrWhiteSpace(sourceFilename)) throw new ArgumentNullException("sourceFilename");
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
                    Arguments = $@"--inform=Video;%FrameRate% ""{sourceFilename}""",
                    RedirectStandardOutput = true,
                    FileName = mediaInfoPath
                }
            };
            mediaInfoProcess.Start();
            mediaInfoProcess.WaitForExit();

            if (mediaInfoProcess.ExitCode != 0)
                throw new Exception($@"MediaInfo returned non-zero exit code: {mediaInfoProcess.ExitCode}");

            return Convert.ToDouble(mediaInfoProcess.StandardOutput.ReadToEnd()) / 1000;
        }
        public static void InsertClientHeartbeat(string machineName)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                using (var scope = new TransactionScope())
                {
                    var updatedRows = connection.Execute(
                        "UPDATE Clients SET LastHeartbeat = @Heartbeat WHERE MachineName = @MachineName;",
                        new {MachineName = machineName, HeartBeat = DateTime.UtcNow});

                    if (updatedRows == 0)
                    {
                        connection.Execute(
                            "INSERT INTO Clients (MachineName, LastHeartbeat) VALUES(@MachineName, @Heartbeat);",
                            new {MachineName = machineName, Heartbeat = DateTime.UtcNow});
                    }

                    scope.Complete();
                }
            }
        }
    }

    public class Mediainfo
    {
        public bool Interlaced { get; set; }

        public double Framerate { get; set; }

        public int Duration { get; set; }
    }
}
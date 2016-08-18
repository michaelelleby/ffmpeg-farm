using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Transactions;
using Contract;
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
                    Arguments = $@"--inform=Video;%Duration%|%FrameRate%|%Width%|%Height%|%ScanType%|%FrameCount% ""{sourceFilename}""",
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
                Duration = Convert.ToInt32(output[0])/1000, // Duration is reported in milliseconds
                Framerate = float.Parse(output[1], NumberFormatInfo.InvariantInfo),
                Width = Convert.ToInt32(output[2]),
                Height = Convert.ToInt32(output[3]),
                Interlaced = output[4].ToUpperInvariant() == "INTERLACED",
                Frames = Convert.ToInt32(output[5])
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
                    Arguments = $@"--inform=Video;%Duration% ""{sourceFilename}""",
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
}
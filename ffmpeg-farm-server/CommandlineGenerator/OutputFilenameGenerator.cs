using System.IO;
using Contract;

namespace CommandlineGenerator
{
    public class OutputFilenameGenerator : IOutputFilenameGenerator
    {
        public string Generate(string prefix, string uniqueNamePart, int bitrate, string extension, string outputFolder)
        {
            string destinationFilename = $@"{prefix}_{uniqueNamePart}_{bitrate}.{extension}";
            string destinationFullPath = $@"{outputFolder}{Path.DirectorySeparatorChar}{destinationFilename}";

            return destinationFullPath;
        }
    }
}
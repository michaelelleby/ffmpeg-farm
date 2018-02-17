using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IGenerator
    {
        string GenerateAudioCommandline(AudioDestinationFormat target, IReadOnlyCollection<string> sourceFilenames, string fullpath);

        string GenerateMuxCommandline(string inputVideoFilename, string inputAudioFilename, string outputFilename,
            TimeSpan inpoint);
    }
}
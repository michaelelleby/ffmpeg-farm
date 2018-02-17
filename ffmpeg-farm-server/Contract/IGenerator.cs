using System.Collections.Generic;

namespace Contract
{
    public interface IGenerator
    {
        string GenerateAudioCommandline(AudioDestinationFormat target, IReadOnlyCollection<string> sourceFilenames, string fullpath);
    }
}
namespace Contract
{
    public interface IOutputFilenameGenerator
    {
        string Generate(string prefix, string uniqueNamePart, int bitrate, string extension, string outputFolder);
    }
}
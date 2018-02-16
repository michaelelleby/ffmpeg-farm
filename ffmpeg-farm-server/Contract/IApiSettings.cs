namespace Contract
{
    public interface IApiSettings
    {
        bool TranscodeToLocalDisk { get; set; }
        bool OverwriteOutput { get; set; }
        bool AbortOnError { get; set; }
    }
}
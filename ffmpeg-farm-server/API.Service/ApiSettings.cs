using Contract;

namespace API.Service
{
    public class ApiSettings : IApiSettings
    {
        public bool TranscodeToLocalDisk { get; set; }
        public bool OverwriteOutput { get; set; }
        public bool AbortOnError { get; set; }
    }
}
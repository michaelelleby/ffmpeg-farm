namespace API.Database
{
    public enum TranscodingJobState : byte
    {
        Unknown = 0,
        Queued = 1,
        Paused = 2,
        InProgress = 3,
        Done = 4,
        Failed = 5,
        Canceled = 6
    }
}
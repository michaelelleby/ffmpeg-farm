namespace Contract.Models
{
    public enum WorkerStatusEnum
    {
        Unknown,
        OK,
        NonResponsive
    }
    public class WorkerStatus
    {
        public string WorkerName { get; set; }
        public WorkerStatusEnum Status { get; set; }
    }
}

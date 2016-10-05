using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

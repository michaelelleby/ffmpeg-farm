using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contract.Models
{
    public class ServiceStatus
    {
        public List<WorkerStatus> Workers { get; set; }

        public ServiceStatus()
        {
            Workers = new List<WorkerStatus>();
        }
    }
}

using System;

namespace Contract
{
    public class ClientHeartbeat
    {
        public string MachineName { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }
}
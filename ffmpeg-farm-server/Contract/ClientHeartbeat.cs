using System;

namespace Contract
{
    public class ClientHeartbeat
    {
        public string MachineName { get; set; }

        public DateTimeOffset LastHeartbeat { get; set; }
    }
}
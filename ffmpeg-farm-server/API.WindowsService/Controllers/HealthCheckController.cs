using System;
using System.Collections.Generic;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;
using Contract.Models;

namespace API.WindowsService.Controllers
{
    public class HealthCheckController : ApiController
    {
        private readonly IHelper _helper;

        public HealthCheckController(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _helper = helper;
        }

        public ServiceStatus Get()
        {
            using (var connection = _helper.GetConnection())
            {
                connection.Open();

                var clients = connection.Query<ClientHeartbeat>("SELECT MachineName, LastHeartbeat FROM Clients;");
                ServiceStatus result = new ServiceStatus();
                DateTime timeLimit = DateTime.UtcNow - TimeSpan.FromMinutes(3);

                foreach (ClientHeartbeat ch in clients)
                {
                    WorkerStatusEnum thisStatus = WorkerStatusEnum.OK;
                    //if (ch.LastHeartbeat < timeLimit)
                    //    thisStatus = WorkerStatusEnum.NonResponsive; 
                    result.Workers.Add(new WorkerStatus() { Status = thisStatus, WorkerName = ch.MachineName });
                }

                return result;
            }
        }
    }
}

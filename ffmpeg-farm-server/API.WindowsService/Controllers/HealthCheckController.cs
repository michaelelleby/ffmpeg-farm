using System;
using System.Collections.Generic;
using System.Web.Http;
using API.Service;
using Contract;
using Dapper;
using Contract.Models;
using System.Configuration;

namespace API.WindowsService.Controllers
{
    public class HealthCheckController : ApiController
    {
        private readonly IHelper _helper;
        private readonly int _workerNonResponsiveAlertMinutes;

        public HealthCheckController(IHelper helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));

            _helper = helper;
            _workerNonResponsiveAlertMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["WorkerNonResponsiveAlertMinutes"]);
        }

        public ServiceStatus Get()
        {
            using (var connection = _helper.GetConnection())
            {
                connection.Open();

                var clients = connection.Query<ClientHeartbeat>("SELECT MachineName, LastHeartbeat FROM Clients;");
                ServiceStatus result = new ServiceStatus();
                DateTime timeLimit = DateTime.UtcNow - TimeSpan.FromMinutes(_workerNonResponsiveAlertMinutes);

                foreach (ClientHeartbeat ch in clients)
                {
                    WorkerStatusEnum thisStatus = WorkerStatusEnum.OK;
                    if (ch.LastHeartbeat < timeLimit)
                        thisStatus = WorkerStatusEnum.NonResponsive; 
                    result.Workers.Add(new WorkerStatus() { Status = thisStatus, WorkerName = ch.MachineName });
                }

                return result;
            }
        }
    }
}

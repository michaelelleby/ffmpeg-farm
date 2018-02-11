using System;
using System.Web.Http;
using Contract;
using Contract.Models;
using System.Configuration;
using API.Database;
using API.Repository;

namespace API.WindowsService.Controllers
{
    public class HealthCheckController : ApiController
    {
        private readonly int _workerNonResponsiveAlertMinutes;

        public HealthCheckController()
        {
            _workerNonResponsiveAlertMinutes = int.Parse(ConfigurationManager.AppSettings["WorkerNonResponsiveAlertMinutes"]);
        }

        [HttpGet]
        public ServiceStatus Get()
        {
            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                ServiceStatus result = new ServiceStatus();
                DateTime timeLimit = DateTime.UtcNow - TimeSpan.FromMinutes(_workerNonResponsiveAlertMinutes);

                foreach (Clients client in unitOfWork.Clients.GetAll())
                {
                    WorkerStatusEnum thisStatus = WorkerStatusEnum.OK;
                    if (client.LastHeartbeat < timeLimit)
                        thisStatus = WorkerStatusEnum.NonResponsive;
                    result.Workers.Add(new WorkerStatus {Status = thisStatus, WorkerName = client.MachineName});
                }

                return result;
            }
        }
    }
}

using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Http;
using API.Database;
using API.Repository;
using Contract;
using Contract.Dto;

namespace API.WindowsService.Controllers
{
    public class TaskController : ApiController
    {
        /// <summary>
        ///     Get next waiting task.
        /// </summary>
        /// <param name="machineName">Caller-id</param>
        [HttpGet]
        public FFmpegTaskDto GetNext(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName)) throw new ArgumentNullException(nameof(machineName));

            using (IUnitOfWork unitOfWork = new UnitOfWork(new FfmpegFarmContext()))
            {
                FfmpegTasks task;

                Clients client = unitOfWork.Clients.Find(c => c.MachineName == machineName).FirstOrDefault()
                                 ?? new Clients { MachineName = machineName };

                client.LastHeartbeat = DateTimeOffset.UtcNow;

                unitOfWork.Complete();

                do
                {
                    try
                    {
                        task = unitOfWork.Tasks.GetNext(TimeSpan.FromMinutes(5));

                        if (task == null)
                            return null;

                        task.TaskState = TranscodingJobState.InProgress;
                        task.Heartbeat = DateTimeOffset.UtcNow;
                        task.HeartbeatMachineName = machineName;

                        unitOfWork.Complete();

                        break;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        ex.Entries.Single().Reload();
                    }
                } while (true);

                return new FFmpegTaskDto
                {
                    State = task.TaskState,
                    Id = task.id,
                    Heartbeat = task.Heartbeat,
                    Started = task.Started,
                    DestinationFilename = task.DestinationFilename,
                    VerifyProgress = task.VerifyProgress,
                    Arguments = task.Arguments,
                    Progress = task.Progress.GetValueOrDefault(),
                    DestinationDurationSeconds = task.DestinationDurationSeconds,
                    HeartbeatMachineName = task.HeartbeatMachineName,
                    VerifyOutput = task.VerifyOutput
                };
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Contract;
using Dapper;
using DR.Common.Monitoring.Models;

namespace API.Repository
{
    public class WorkerNodesHealthCheck : CommonHealthCheck
    {
        private readonly IHelper _helper;
        private readonly int _windowInMinutes;
        private readonly int _minimumErrors;
        private readonly float _minimumErrorRate;

        public WorkerNodesHealthCheck(
            IHelper helper,
            int windowInMinutes,
            int minimumErrors,
            float minimumErrorRate
        
        ) : base($"OD3.FfmpegFarm.{nameof(WorkerNodesHealthCheck)}",
            descriptionText: "Detects failing nodes")
        {
            _helper = helper;
            _windowInMinutes = windowInMinutes;
            _minimumErrors = minimumErrors;
            _minimumErrorRate = minimumErrorRate;
        }

        protected override void RunTest(StatusBuilder statusBuilder)
        {
            var filter = DateTimeOffset.UtcNow.AddMinutes(-_windowInMinutes);
            using (var scope = TransactionUtils.CreateTransactionScope(IsolationLevel.ReadUncommitted))
            using (var connection = _helper.GetConnection())
            {
                connection.Open();
                var res = connection.Query<WorkerNodeStatus>(@"
SELECT COUNT(*) AS [Count], [t0].[TaskState] AS [State], [t0].[HeartbeatMachineName]
FROM [FfmpegTasks] AS [t0]
WHERE [t0].[Heartbeat] > @Filter
GROUP BY [t0].[TaskState], [t0].[HeartbeatMachineName]",
                    new {Filter = filter}).ToArray();

                var doneCount = res
                    .Where(x => x.State == TranscodingJobState.Done)
                    .Aggregate(0, (x, y) => y.Count + x);

                statusBuilder.MessageBuilder.AppendLine(
                    $" In the last {_windowInMinutes} minutes {doneCount} task(s) has completed successfully. ");

                if (res.Any(x => x.State == TranscodingJobState.Failed))
                {
                    statusBuilder.CurrentLevel = SeverityLevel.Warning;
                    statusBuilder.Passed = false;

                    var failureCount = res
                        .Where(x => x.State == TranscodingJobState.Failed)
                        .Aggregate(0, (x, y) => y.Count + x);

                    statusBuilder.MessageBuilder.AppendLine($" Warning: {failureCount} failures detected ");
                }
                else
                {
                    statusBuilder.Passed = true;
                    return;
                }

                
                foreach (var failingNode in res.Where(x => x.State == TranscodingJobState.Failed))
                {
                    var doneTaskCountForNode = res.SingleOrDefault(x =>
                        x.HeartbeatMachineName == failingNode.HeartbeatMachineName &&
                        x.State == TranscodingJobState.Done)?.Count ?? 0;

                    var ratio = failingNode.Count / ((float) failingNode.Count + doneTaskCountForNode);

                    if (failingNode.Count >= _minimumErrors && ratio >= _minimumErrorRate)
                    {
                        statusBuilder.CurrentLevel = SeverityLevel.Error;
                        statusBuilder.MessageBuilder.AppendLine(
                            $" Alarm triggered for {failingNode.HeartbeatMachineName} , error count {failingNode.Count} , error ratio {ratio * 100:F1}% ");
                    }
                    else
                    {
                        statusBuilder.MessageBuilder.AppendLine(
                            $" Warning for {failingNode.HeartbeatMachineName} , error count {failingNode.Count} , error ratio {ratio * 100:F1}% ");
                    }
                }
                scope.Complete();
            }
        }

        public class WorkerNodeStatus
        {
            public int Count { get; set; }
            public TranscodingJobState State { get; set; }
            public string HeartbeatMachineName { get; set; }
        }
    }
}

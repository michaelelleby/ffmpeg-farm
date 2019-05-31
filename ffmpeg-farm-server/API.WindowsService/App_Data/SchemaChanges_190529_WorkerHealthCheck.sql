SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE NONCLUSTERED INDEX IX_WorkerNodeHealthCheck
ON [dbo].[FfmpegTasks] ([Heartbeat])
INCLUDE ([TaskState],[HeartbeatMachineName])
GO


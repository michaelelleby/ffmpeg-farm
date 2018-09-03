/****** Object:  StoredProcedure [dbo].[sp_GetNextTask]    Script Date: 03-09-2018 13:43:42 ******/
/****** Fixes missing HeartbeatMachineName when updating Task. This was causing multiple stereotool jobs to run on the same node ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[sp_GetNextTask_v2]
	@Timestamp DATETIMEOFFSET,
	@QueuedState INT,
	@InProgressState INT,
	@Timeout DATETIMEOFFSET,
	@MachineName VARCHAR(50)
AS
BEGIN
    -- v2 Fixes missing HeartbeatMachineName when updating Task. This was causing multiple stereotool jobs to run on the same node
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @TaskID INT, @JobId INT
	
	-- Use a CTE to enable updating and reading in one operation, to prevent multiple operations from updating the same rows
	;WITH Tasks AS (
		SELECT TOP 1 FfmpegTasks.Id, Arguments, TaskState, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename, Jobs.id AS FfmpegJobs_Id
		FROM FfmpegTasks
		INNER JOIN FfmpegJobs Jobs ON FfmpegTasks.FfmpegJobs_id = Jobs.id
		WHERE TaskState = @QueuedState OR (TaskState = @InProgressState AND HeartBeat < @Timeout)
		ORDER BY Jobs.Needed ASC, Jobs.Id ASC
	)

	UPDATE Tasks SET TaskState = @InProgressState, Started = @Timestamp, HeartbeatMachineName = @MachineName, Heartbeat = @Timestamp, @TaskId = Id, @JobId = FFmpegJobs_Id;

	-- Mark FfmpegJobs row as InProgress, if it is not already set to InProgress
	IF @@ROWCOUNT > 0
	BEGIN
		UPDATE FfmpegJobs SET JobState = @InProgressState WHERE Id = @JobId AND JobState != @InProgressState;
	END

	SELECT id, FfmpegJobs_id AS FfmpegJobsId, FfmpegExePath, Arguments, TaskState, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename, VerifyOutput FROM FfmpegTasks WHERE Id = @TaskId;
END

GO
GRANT EXECUTE ON [dbo].[sp_GetNextTask_v2] to ffmpegtst_RW
GO
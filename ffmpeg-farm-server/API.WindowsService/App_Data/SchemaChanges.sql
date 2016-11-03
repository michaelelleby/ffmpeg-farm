ALTER TABLE FfmpegTasks
	ADD VerifyOutput BIT NOT NULL DEFAULT(0)
GO

ALTER PROCEDURE [dbo].[sp_GetNextTask]
	@Timestamp DATETIMEOFFSET,
	@QueuedState INT,
	@InProgressState INT,
	@Timeout DATETIMEOFFSET
AS
BEGIN
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

	UPDATE Tasks SET TaskState = @InProgressState, Started = @Timestamp, Heartbeat = @Timestamp, @TaskId = Id, @JobId = FFmpegJobs_Id;

	-- Mark FfmpegJobs row as InProgress, if it is not already set to InProgress
	IF @@ROWCOUNT > 0
	BEGIN
		UPDATE FfmpegJobs SET JobState = @InProgressState WHERE Id = @JobId AND JobState != @InProgressState;
	END

	SELECT id, FfmpegJobs_id AS FfmpegJobsId, Arguments, TaskState, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename, VerifyOutput FROM FfmpegTasks WHERE Id = @TaskId;
END

GO
ALTER TABLE [dbo].[FfmpegTasks]
 ADD VersionColumn ROWVERSION
GO

ALTER PROCEDURE [dbo].[sp_GetNextTask]
	@Timestamp DATETIMEOFFSET,
	@QueuedState INT,
	@InProgressState INT,
	@Timeout DATETIMEOFFSET
AS
BEGIN
	DECLARE @TaskID INT = 0, @RowVer ROWVERSION

	WHILE 1 = 1
	BEGIN
		SELECT TOP 1 @TaskID = FfmpegTasks.Id, @RowVer = VersionColumn
			FROM FfmpegTasks
			INNER JOIN FfmpegJobs Jobs ON FfmpegTasks.FfmpegJobs_id = Jobs.id
			WHERE TaskState = @QueuedState OR (TaskState = @InProgressState AND HeartBeat < @Timeout)
			ORDER BY Jobs.Needed ASC, Jobs.Id ASC

		-- No tasks found
		IF @TaskID = 0
			BREAK

		UPDATE FfmpegTasks SET TaskState = @InProgressState WHERE Id = @TaskID AND VersionColumn = @RowVer
		IF @@ROWCOUNT = 0
		BEGIN
			-- If @@ROWCOUNT = 0 this means someone else updated the row between when we read it and tried to update it,
			-- because this would change the VersionColumn column
			SET @TaskID = 0
			CONTINUE
		END
		ELSE
		BEGIN
			SELECT id, FfmpegJobs_id AS FfmpegJobsId, Arguments, TaskState, Started, Heartbeat, HeartbeatMachineName, Progress, DestinationFilename, VerifyOutput FROM FfmpegTasks WHERE Id = @TaskID
			BREAK
		END
	END
END
GO
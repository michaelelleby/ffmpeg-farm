/****** Object:  Table [dbo].[Clients]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Clients](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MachineName] [nvarchar](50) NOT NULL,
	[LastHeartbeat] [datetimeoffset](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegAudioRequest]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegAudioRequest](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[SourceFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NULL,
	[Created] [datetimeoffset](7) NOT NULL,
	[OutputFolder] [varchar](max) NOT NULL,
 CONSTRAINT [PK_FfmpegAudioRequest] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegAudioRequestTargets]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegAudioRequestTargets](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Codec] [varchar](50) NOT NULL,
	[Format] [varchar](50) NOT NULL,
	[Bitrate] [int] NOT NULL,
 CONSTRAINT [PK_FfmpegAudioRequestTargets] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegHardSubtitlesRequest]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegHardSubtitlesRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[VideoSourceFilename] [nvarchar](max) NOT NULL,
	[SubtitlesFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_FfmpegHardSubtitlesRequest] PRIMARY KEY CLUSTERED 
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegJobs]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegJobs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[JobType] [tinyint] NOT NULL,
	[JobState] [tinyint] NOT NULL,
 CONSTRAINT [PK_FfmpegJobs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegMuxRequest]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegMuxRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[VideoSourceFilename] [nvarchar](max) NOT NULL,
	[AudioSourceFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_FfmpegMuxRequest] PRIMARY KEY CLUSTERED 
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegTasks]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegTasks](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[FfmpegJobs_id] [int] NOT NULL,
	FfmpegExePath NVARCHAR(500) NULL,
	[Arguments] [nvarchar](max) NOT NULL,
	[TaskState] [tinyint] NOT NULL,
	[DestinationDurationSeconds] [int] NOT NULL,
	[Started] [datetimeoffset](7) NULL,
	[Heartbeat] [datetimeoffset](7) NULL,
	[HeartbeatMachineName] [varchar](50) NULL,
	[Progress] [float] NULL,
	[DestinationFilename] [nvarchar](max) NULL,
	[VerifyOutput] [bit] NOT NULL DEFAULT ((0)),
	[VerifyProgress] [float] NULL,
 CONSTRAINT [PK_FfmpegMuxTasks] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegVideoJobs]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegVideoJobs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Progress] [float] NOT NULL,
	[Heartbeat] [datetime] NULL,
	[Arguments] [nvarchar](max) NOT NULL,
	[Needed] [datetime] NULL,
	[VideoSourceFilename] [nvarchar](max) NULL,
	[AudioSourceFilename] [nvarchar](max) NULL,
	[ChunkDuration] [float] NOT NULL,
	[HeartbeatMachineName] [nvarchar](max) NULL,
	[State] [varchar](50) NOT NULL,
	[Started] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegVideoMergeJobs]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegVideoMergeJobs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Progress] [float] NOT NULL,
	[Heartbeat] [datetime] NULL,
	[Arguments] [nvarchar](max) NOT NULL,
	[Needed] [datetime] NULL,
	[HeartbeatMachineName] [nvarchar](max) NULL,
	[State] [varchar](50) NOT NULL,
	[Target] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegVideoParts]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegVideoParts](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Filename] [nvarchar](max) NOT NULL,
	[Number] [int] NOT NULL,
	[Target] [int] NOT NULL,
	[FfmpegJobs_Id] [int] NOT NULL,
	[PSNR] [float] NOT NULL,
	[Width] [int] NOT NULL,
	[Height] [int] NOT NULL,
	[Bitrate] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegVideoRequest]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegVideoRequest](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[VideoSourceFilename] [nvarchar](max) NULL,
	[AudioSourceFilename] [nvarchar](max) NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetime] NOT NULL,
	[Created] [datetime] NULL,
	[EnableDash] [bit] NOT NULL,
	[EnableTwoPass] [bit] NOT NULL,
	[EnablePsnr] [bit] NOT NULL,
	[Inpoint] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
 CONSTRAINT [IX_FfmpegRequest] UNIQUE NONCLUSTERED 
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegVideoRequestTargets]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING OFF
GO
CREATE TABLE [dbo].[FfmpegVideoRequestTargets](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Width] [int] NOT NULL,
	[Height] [int] NOT NULL,
	[VideoBitrate] [int] NOT NULL,
	[AudioBitrate] [int] NOT NULL,
	[H264Profile] [varchar](255) NOT NULL,
	[H264Level] [varchar](3) NOT NULL,
 CONSTRAINT [PK_FfmpegRequestTargets] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Log]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Log](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Application] [nvarchar](50) NOT NULL,
	[Logged] [datetime] NOT NULL,
	[Level] [nvarchar](50) NOT NULL,
	[Message] [nvarchar](max) NOT NULL,
	[UserName] [nvarchar](250) NULL,
	[ServerName] [nvarchar](max) NULL,
	[Port] [nvarchar](max) NULL,
	[Url] [nvarchar](max) NULL,
	[Https] [bit] NULL,
	[ServerAddress] [nvarchar](100) NULL,
	[RemoteAddress] [nvarchar](100) NULL,
	[Logger] [nvarchar](250) NULL,
	[Callsite] [nvarchar](max) NULL,
	[Exception] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo.Log] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Mp4boxJobs]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Mp4boxJobs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Heartbeat] [datetime] NULL,
	[Arguments] [nvarchar](max) NOT NULL,
	[Needed] [datetime] NULL,
	[HeartbeatMachineName] [nvarchar](max) NULL,
	[State] [varchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

/****** Object:  Table [dbo].[FfmpegScreenshotRequest]    Script Date: 23-03-2018 10:08:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FfmpegScreenshotRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[SourceFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

SET ANSI_PADDING OFF
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_Clients]    Script Date: 06-02-2018 14:49:29 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_Clients] ON [dbo].[Clients]
(
	[MachineName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegAudioRequest]    Script Date: 06-02-2018 14:49:29 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_FfmpegAudioRequest] ON [dbo].[FfmpegAudioRequest]
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegJobs_Id_JobState]    Script Date: 21-03-2017 15:19:45 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegJobs_Id_JobState] ON [dbo].[FfmpegJobs]
(
	[id] ASC,
	[JobState] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegJobs_Id_Needed]    Script Date: 21-03-2017 15:19:45 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegJobs_Id_Needed] ON [dbo].[FfmpegJobs]
(
	[id] ASC,
	[Needed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegTasks]    Script Date: 06-02-2018 14:49:29 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegTasks] ON [dbo].[FfmpegTasks]
(
	[TaskState] ASC,
	[Heartbeat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegTasks_JobsId]    Script Date: 06-02-2018 14:49:29 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegTasks_JobsId] ON [dbo].[FfmpegTasks]
(
	[FfmpegJobs_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegTasks_Id_TaskState]    Script Date: 21-03-2017 15:19:45 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegTasks_Id_TaskState] ON [dbo].[FfmpegTasks]
(
	[id] ASC,
	[TaskState] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegTasks_TaskState_Heartbeat]    Script Date: 21-03-2017 15:19:45 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegTasks_TaskState_Heartbeat] ON [dbo].[FfmpegTasks]
(
	[TaskState] ASC,
	[Heartbeat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
ALTER TABLE [dbo].[FfmpegVideoJobs] ADD  DEFAULT ((0)) FOR [Progress]
GO
ALTER TABLE [dbo].[FfmpegVideoMergeJobs] ADD  DEFAULT ((0)) FOR [Progress]
GO
ALTER TABLE [dbo].[FfmpegVideoParts] ADD  DEFAULT ((0)) FOR [PSNR]
GO
ALTER TABLE [dbo].[FfmpegVideoRequest] ADD  CONSTRAINT [DF_FfmpegRequest_EnableDash]  DEFAULT ((0)) FOR [EnableDash]
GO
ALTER TABLE [dbo].[FfmpegVideoRequest] ADD  CONSTRAINT [DF_FfmpegRequest_EnableTwoPass]  DEFAULT ((0)) FOR [EnableTwoPass]
GO
ALTER TABLE [dbo].[FfmpegVideoRequest] ADD  CONSTRAINT [DF_FfmpegRequest_EnablePsnr]  DEFAULT ((0)) FOR [EnablePsnr]
GO
ALTER TABLE [dbo].[FfmpegTasks]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegTasks_FfmpegJobs] FOREIGN KEY([FfmpegJobs_id])
REFERENCES [dbo].[FfmpegJobs] ([id])
GO
ALTER TABLE [dbo].[FfmpegTasks] CHECK CONSTRAINT [FK_FfmpegTasks_FfmpegJobs]
GO
ALTER TABLE [dbo].[FfmpegVideoJobs]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegVideoRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegVideoJobs] CHECK CONSTRAINT [FK_FfmpegJobs_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegVideoMergeJobs]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegMergeJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegVideoRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegVideoMergeJobs] CHECK CONSTRAINT [FK_FfmpegMergeJobs_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegVideoParts]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegParts_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegVideoRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegVideoParts] CHECK CONSTRAINT [FK_FfmpegParts_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegVideoRequestTargets]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegRequestTargets_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegVideoRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegVideoRequestTargets] CHECK CONSTRAINT [FK_FfmpegRequestTargets_FfmpegRequest]
GO
ALTER TABLE [dbo].[Mp4boxJobs]  WITH CHECK ADD  CONSTRAINT [FK_Mp4boxJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegVideoRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[Mp4boxJobs] CHECK CONSTRAINT [FK_Mp4boxJobs_FfmpegRequest]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetNextTask]    Script Date: 03-09-2018 13:43:42 ******/
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
/****** Object:  StoredProcedure [dbo].[sp_InsertClientHeartbeat]    Script Date: 06-02-2018 14:49:29 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Michael Christiansen, MCHA
-- Create date: 2016-10-14
-- Description:	Register client heartbeat, updating existing row if client has already registered a heartbeat previously
-- =============================================
CREATE PROCEDURE [dbo].[sp_InsertClientHeartbeat]
	@MachineName NVARCHAR(50),
	@Timestamp DATETIMEOFFSET
AS

BEGIN
	UPDATE	Clients
		SET		LastHeartbeat = @Timestamp
		WHERE	MachineName = @MachineName

	IF @@ROWCOUNT = 0
	BEGIN
		INSERT INTO Clients (MachineName, LastHeartbeat)
		VALUES		(@MachineName, @Timestamp)
	END

	RETURN @@ROWCOUNT
END


GO


CREATE NONCLUSTERED INDEX IX_WorkerNodeHealthCheck
ON [dbo].[FfmpegTasks] ([Heartbeat])
INCLUDE ([TaskState],[HeartbeatMachineName])
GO


CREATE NONCLUSTERED INDEX IX_Janitor
ON [dbo].[FfmpegJobs] ([Created])
INCLUDE ([JobCorrelationId])
GO

CREATE NONCLUSTERED INDEX IX_Janitor_2
ON [dbo].[FfmpegJobs] ([JobCorrelationId],[Created])

GO

CREATE NONCLUSTERED INDEX IX_Logged
ON [dbo].[Log] ([Logged])

GO
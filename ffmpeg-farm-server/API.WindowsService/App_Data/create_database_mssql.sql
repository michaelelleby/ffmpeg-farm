/****** Object:  User [ffmpegfarm]    Script Date: 06-10-2016 14:28:34 ******/
CREATE USER [ffmpegfarm] FOR LOGIN [ffmpegfarm] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_datareader] ADD MEMBER [ffmpegfarm]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [ffmpegfarm]
GO
/****** Object:  Table [dbo].[Clients]    Script Date: 06-10-2016 14:28:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Clients](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MachineName] [nvarchar](max) NOT NULL,
	[LastHeartbeat] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegAudioJobs]    Script Date: 06-10-2016 14:28:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegAudioJobs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Arguments] [varchar](500) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[SourceFilename] [nvarchar](max) NOT NULL,
	[State] [varchar](50) NOT NULL,
	[Started] [datetimeoffset](7) NULL,
	[Heartbeat] [datetimeoffset](7) NULL,
	[HeartbeatMachineName] [varchar](50) NULL,
	[Progress] [float] NULL,
	[DestinationFilename] [varchar](500) NOT NULL,
	[Bitrate] [int] NOT NULL,
 CONSTRAINT [PK_FfmpegAudioJobs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[FfmpegAudioRequest]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegAudioRequestTargets]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegVideoJobs]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegVideoMergeJobs]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegVideoParts]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegVideoRequest]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[FfmpegVideoRequestTargets]    Script Date: 06-10-2016 14:28:34 ******/
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
/****** Object:  Table [dbo].[Mp4boxJobs]    Script Date: 06-10-2016 14:28:34 ******/
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
SET ANSI_PADDING OFF
GO
SET ANSI_PADDING ON

GO
/****** Object:  Index [IX_FfmpegAudioJobs]    Script Date: 06-10-2016 14:28:34 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegAudioJobs] ON [dbo].[FfmpegAudioJobs]
(
	[id] ASC,
	[State] ASC,
	[Heartbeat] ASC,
	[Needed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
/****** Object:  Index [IX_FfmpegAudioRequest]    Script Date: 06-10-2016 14:28:34 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_FfmpegAudioRequest] ON [dbo].[FfmpegAudioRequest]
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
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
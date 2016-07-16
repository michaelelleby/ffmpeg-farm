USE [ffmpegfarm]
GO
/****** Object:  Table [dbo].[Clients]    Script Date: 16-07-2016 07:28:16 ******/
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
/****** Object:  Table [dbo].[FfmpegJobs]    Script Date: 16-07-2016 07:28:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegJobs](
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
/****** Object:  Table [dbo].[FfmpegMergeJobs]    Script Date: 16-07-2016 07:28:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[FfmpegMergeJobs](
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
/****** Object:  Table [dbo].[FfmpegParts]    Script Date: 16-07-2016 07:28:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegParts](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[Target] [int] NOT NULL,
	[Filename] [nvarchar](max) NOT NULL,
	[Number] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[FfmpegRequest]    Script Date: 16-07-2016 07:28:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegRequest](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[VideoSourceFilename] [nvarchar](max) NULL,
	[AudioSourceFilename] [nvarchar](max) NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetime] NOT NULL,
	[Created] [datetime] NULL,
	[EnableDash] [bit] NOT NULL,
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
/****** Object:  Table [dbo].[FfmpegRequestTargets]    Script Date: 16-07-2016 07:28:16 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING OFF
GO
CREATE TABLE [dbo].[FfmpegRequestTargets](
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
/****** Object:  Table [dbo].[Mp4boxJobs]    Script Date: 16-07-2016 07:28:16 ******/
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
ALTER TABLE [dbo].[FfmpegJobs] ADD  DEFAULT ((0)) FOR [Progress]
GO
ALTER TABLE [dbo].[FfmpegMergeJobs] ADD  DEFAULT ((0)) FOR [Progress]
GO
ALTER TABLE [dbo].[FfmpegJobs]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegJobs] CHECK CONSTRAINT [FK_FfmpegJobs_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegMergeJobs]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegMergeJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegMergeJobs] CHECK CONSTRAINT [FK_FfmpegMergeJobs_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegParts]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegParts_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegParts] CHECK CONSTRAINT [FK_FfmpegParts_FfmpegRequest]
GO
ALTER TABLE [dbo].[FfmpegRequestTargets]  WITH CHECK ADD  CONSTRAINT [FK_FfmpegRequestTargets_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[FfmpegRequestTargets] CHECK CONSTRAINT [FK_FfmpegRequestTargets_FfmpegRequest]
GO
ALTER TABLE [dbo].[Mp4boxJobs]  WITH CHECK ADD  CONSTRAINT [FK_Mp4boxJobs_FfmpegRequest] FOREIGN KEY([JobCorrelationId])
REFERENCES [dbo].[FfmpegRequest] ([JobCorrelationId])
GO
ALTER TABLE [dbo].[Mp4boxJobs] CHECK CONSTRAINT [FK_Mp4boxJobs_FfmpegRequest]
GO

/****** Object:  Table [dbo].[FfmpegScreenshotRequest]    Script Date: 23-03-2018 10:08:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FfmpegLoudnessRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[SourceFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


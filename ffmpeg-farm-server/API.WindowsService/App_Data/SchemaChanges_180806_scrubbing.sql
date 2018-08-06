SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FfmpegScrubbingRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[SourceFilename] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL,
	[SpriteSheetSizes] [nvarchar](max) NOT NULL,
	[ThumbnailResoultions] [nvarchar](max) NOT NULL,
	[FirstThumbnailOffsetInSeconds] [int] NOT NULL,
	[MaxSecondsBetweenThumbnails] [int] NOT NULL,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

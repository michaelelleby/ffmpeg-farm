/****** Object:  Table [dbo].[FfmpegDeinterlacingRequest]    Script Date: 10-02-2020 12:22:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FfmpegDeinterlacingRequest](
	[JobCorrelationId] [uniqueidentifier] NOT NULL,
	[VideoSourceFilename] [nvarchar](max) NOT NULL,
	[DestinationFilename] [nvarchar](max) NOT NULL,
	[OutputFolder] [nvarchar](max) NOT NULL,
	[Needed] [datetimeoffset](7) NOT NULL,
	[Created] [datetimeoffset](7) NOT NULL,
 CONSTRAINT [PK_FfmpegDeinterlacingRequest] PRIMARY KEY CLUSTERED 
(
	[JobCorrelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
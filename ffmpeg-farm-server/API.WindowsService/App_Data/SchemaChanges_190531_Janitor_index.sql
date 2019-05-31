
CREATE NONCLUSTERED INDEX IX_Janitor
ON [dbo].[FfmpegJobs] ([Created])
INCLUDE ([JobCorrelationId])
GO

CREATE NONCLUSTERED INDEX IX_Janitor_2
ON [dbo].[FfmpegJobs] ([JobCorrelationId],[Created])

GO
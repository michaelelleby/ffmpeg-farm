EXEC sp_rename N'FfmpegTasks.IX_FfmpegTasks', N'IX_FfmpegTasks_TaskState_Heartbeat', N'INDEX';
GO

EXEC sp_rename N'FfmpegTasks.IX_FfmpegTasks_JobsId', N'[IX_FfmpegTasks_FfmpegJobs_id]', N'INDEX';
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
/****** Object:  Index [IX_FfmpegTasks_TaskState_Heartbeat]    Script Date: 21-03-2017 15:19:45 ******/
CREATE NONCLUSTERED INDEX [IX_FfmpegTasks_TaskState_Heartbeat] ON [dbo].[FfmpegTasks]
(
	[TaskState] ASC,
	[Heartbeat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
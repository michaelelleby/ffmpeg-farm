BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "FfmpegRequest" (
	`Id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`JobCorrelationId`	UNIQUEIDENTIFIER NOT NULL,
	`VideoSourceFilename`	TEXT,
	`AudioSourceFilename`	TEXT,
	`DestinationFilename`	TEXT NOT NULL,
	`Needed`	DATETIME NOT NULL,
	`Created`	DATETIME,
	`EnableDash` BIT NOT NULL
);
CREATE TABLE IF NOT EXISTS "FfmpegParts" (
	`id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`JobCorrelationId`	UNIQUEIDENTIFIER NOT NULL,
	`Target`	INTEGER NOT NULL,
	`Filename`	TEXT NOT NULL,
	`Number`	INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS "FfmpegJobs" (
	`Id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`JobCorrelationId`	uniqueidentifier NOT NULL,
	`Progress`	REAL NOT NULL DEFAULT 0,
	`Taken`	bit NOT NULL DEFAULT 0,
	`Heartbeat`	datetime,
	`Arguments`	TEXT NOT NULL,
	`Done`	INTEGER NOT NULL DEFAULT 0,
	`Active` bit NOT NULL DEFAULT 1,
	`Needed`	datetime,
	`VideoSourceFilename`	TEXT,
	`AudioSourceFilename`	TEXT,
	`ChunkDuration`	REAL NOT NULL,
	`HeartbeatMachineName`	TEXT
);
CREATE TABLE IF NOT EXISTS "FfmpegMergeJobs" (
	`Id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`JobCorrelationId`	uniqueidentifier NOT NULL,
	`Progress`	REAL NOT NULL DEFAULT 0,
	`Taken`	bit NOT NULL DEFAULT 0,
	`Heartbeat`	datetime,
	`Arguments`	TEXT NOT NULL,
	`Done`	INTEGER NOT NULL DEFAULT 0,
	`Active` bit NOT NULL DEFAULT 1,
	`Needed`	datetime,
	`HeartbeatMachineName`	TEXT
);
CREATE TABLE IF NOT EXISTS "Mp4boxJobs" (
	`Id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`JobCorrelationId`	uniqueidentifier NOT NULL,
	`Taken`	bit NOT NULL DEFAULT 0,
	`Heartbeat`	datetime,
	`Arguments`	TEXT NOT NULL,
	`Done`	INTEGER NOT NULL DEFAULT 0,
	`Active` bit NOT NULL DEFAULT 1,
	`Needed`	datetime,
	`HeartbeatMachineName`	TEXT
);
CREATE TABLE IF NOT EXISTS "Clients" (
	`id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
	`MachineName`	TEXT NOT NULL UNIQUE,
	`LastHeartbeat`	datetime NOT NULL
);
COMMIT;

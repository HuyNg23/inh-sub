-- Create store sp_UpdateStatusServerSchedule
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateStatusServerSchedule]') AND type in (N'P'))
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_UpdateStatusServerSchedule]
        @status bit, @site nvarchar(256)
    AS
    BEGIN
        UPDATE S_ServerScheduleStatus SET Status = @status, ModificationDate = GETDATE() WHERE Site = @site
    END
    ');
    PRINT 'Stored Procedure sp_UpdateStatusServerSchedule created.'
END

-- Create store procedure sp_UpdateStatusServerShrinkLog
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_UpdateStatusServerShrinkLog]') AND type in (N'P'))
BEGIN
    EXEC('
    Create PROCEDURE [dbo].[sp_UpdateStatusServerShrinkLog]
	@status bit, @site nvarchar(256)
	AS
	BEGIN
		update S_ServerScheduleShrinkLogStates set Status = @status, ModificationDate = getdate() where Site = @site
	END
    ');
    PRINT 'Stored Procedure sp_UpdateStatusServerShrinkLog created.'
END

-- Create table C_ConfigRoot
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[C_ConfigRoot]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[C_ConfigRoot](
		[Id] [nvarchar](60) NOT NULL,
		[NameConfig] [nvarchar](256) NULL,
		[ValueConfig] [nvarchar](max) NULL,
		[TypeConfig] [nvarchar](256) NULL,
		[CreatedDate] [datetime2](7) NULL,
		[IsDelete] [bit] NULL,
		[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_C_ConfigRoot] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table C_ConfigRoot created.'
END

-- Create table D_DatabaseConnections
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[D_DatabaseConnections]') AND type in (N'U'))
BEGIN
   CREATE TABLE [dbo].[D_DatabaseConnections](
	[Id] [nvarchar](60) NOT NULL,
	[Address] [nvarchar](max) NOT NULL,
	[Username] [nvarchar](max) NOT NULL,
	[Password] [nvarchar](max) NOT NULL,
	[DeploymentType] [int] NOT NULL,
	[Timeout] [int] NULL,
	[PathSaveLog] [nvarchar](max) NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_DatabaseConnections] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table D_DatabaseConnections created.'
END

-- Create table S_PlanShrinkLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[S_PlanShrinkLogs]') AND type in (N'U'))
BEGIN
   CREATE TABLE [dbo].[S_PlanShrinkLogs](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[ScheduleId] [nvarchar](60) NULL,
	[ListCategory] [nvarchar](max) NULL,
	[StatusPlan] [int] NULL,
	[FormatCron] [nvarchar](2048) NULL,
	[KeyTrigg] [nvarchar](256) NULL,
	[Site] [nvarchar](64) NULL,
	[SiteRunning] [nvarchar](64) NULL,
	[Type] [int] NULL,
	[DeploymentType] [int] NULL,
	[MethodShrinkLog] [int] NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_PlanShrinkLogs] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table S_PlanShrinkLogs created.'
END

-- Create table S_ScheduleShrinkLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[S_ScheduleShrinkLogs]') AND type in (N'U'))
BEGIN
   CREATE TABLE [dbo].[S_ScheduleShrinkLogs](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](2048) NULL,
	[ListCategory] [nvarchar](max) NULL,
	[Type] [int] NULL,
	[Day] [nvarchar](2) NULL,
	[Month] [nvarchar](2) NULL,
	[Year] [nvarchar](4) NULL,
	[Weekday] [nvarchar](64) NULL,
	[Hour] [nvarchar](2) NULL,
	[Minute] [nvarchar](2) NULL,
	[Site] [nvarchar](64) NULL,
	[MethodShrinkLog] [int] NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_ScheduleShrinkLogs] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table S_ScheduleShrinkLogs created.'
END

-- Create table S_ServerScheduleShrinkLogStates
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[S_ServerScheduleShrinkLogStates]') AND type in (N'U'))
BEGIN
   CREATE TABLE [dbo].[S_ServerScheduleShrinkLogStates](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Site] [nvarchar](max) NULL,
	[Status] [bit] NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_S_ServerScheduleShrinkLogStates] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table S_ServerScheduleShrinkLogStates created.'
END

-- Create table S_ServerScheduleStatus
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[S_ServerScheduleStatus]') AND type in (N'U'))
BEGIN
   CREATE TABLE [dbo].[S_ServerScheduleStatus](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Site] [nvarchar](max) NULL,
	[Status] [bit] NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	 CONSTRAINT [PK_S_ServerScheduleStatus] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT 'Table S_ServerScheduleStatus created.'
END

-- Delete column Level, Port, Status in table S_ServerSites
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[S_ServerSites]') AND type in (N'U'))
BEGIN
	IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'S_ServerSites') 
           AND name = 'Level')
	BEGIN
		ALTER TABLE S_ServerSites
		DROP COLUMN Level;

		PRINT 'Column Level dropped from S_ServerSites.'
	END

	IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'ServerSites') 
           AND name = 'Port')
	BEGIN
		ALTER TABLE S_ServerSites
		DROP COLUMN Port;

		PRINT 'Column Port dropped from S_ServerSites.'
	END

	IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'S_ServerSites') 
           AND name = 'Status')
	BEGIN
		ALTER TABLE S_ServerSites
		DROP COLUMN Status;

		PRINT 'Column Status dropped from S_ServerSites.'
	END
END

-- Add column table P_Package_Details
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P_Package_Details]') AND type in (N'U'))
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'P_Package_Details') 
           AND name = 'Name')
	BEGIN
		ALTER TABLE [dbo].[P_Package_Details]
		ADD [Name] nvarchar(450) NOT NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'P_Package_Details') 
           AND name = 'Path')
	BEGIN
		ALTER TABLE [dbo].[P_Package_Details]
		ADD [Path] nvarchar(max) NOT NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'P_Package_Details') 
           AND name = 'Version')
	BEGIN
		ALTER TABLE [dbo].[P_Package_Details]
		ADD [Version] nvarchar(max) NOT NULL;
	END
	PRINT 'Update table P_Package_Details success.'
END

-- Add column table P_Packages
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P_Packages]') AND type in (N'U'))
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'P_Packages') 
           AND name = 'Name')
	BEGIN
		ALTER TABLE [dbo].[P_Packages]
		ADD [Name] nvarchar(450) NOT NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'P_Packages') 
           AND name = 'Description')
	BEGIN
		ALTER TABLE [dbo].[P_Packages]
		ADD [Description] nvarchar(max) NULL;
	END
	PRINT 'Update table P_Packages success.'
END

-- Add column table T_Tenants
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Tenants]') AND type in (N'U'))
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_Host')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_Host] [nvarchar](max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_Port')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_Port] [nvarchar](max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_Username')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_Username] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_Password')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_Password] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_Credentials')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_Credentials] [bit] NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'Smtp_EnableSSL')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [Smtp_EnableSSL] [bit] NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'MailTo')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [MailTo] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'MailCC')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [MailCC] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'MailBCC')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [MailBCC] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'MailTitle')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [MailTitle] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'MailBody')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [MailBody] nvarchar(max) NULL;
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'T_Tenants') 
           AND name = 'AutoSendMail')
	BEGIN
		ALTER TABLE [dbo].[T_Tenants]
		ADD [AutoSendMail] [bit] NULL;
	END

	PRINT 'Update table T_Tenants success.'
END

-- Delete table T_Tenant_Change_Info_Histories
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Tenant_Change_Info_Histories]') AND type in (N'U'))
BEGIN
   DROP TABLE [dbo].[T_Tenant_Change_Info_Histories];
   PRINT 'Table T_Tenant_Change_Info_Histories deleted.'
END
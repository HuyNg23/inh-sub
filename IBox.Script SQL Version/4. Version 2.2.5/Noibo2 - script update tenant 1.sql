DECLARE @dbName NVARCHAR(255);
DECLARE @sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR
SELECT name 
FROM sys.databases 
WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'IBOX_MA01', 'IBOX_MA05', 'IBOX_MA06', 'IBOX_MA07', 'IBOX_MA20');

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
set @sql = 'USE [' + @dbName + '];

-- Update column StepId in table [H_ApiThirdPartyExecuteHistorys]
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[H_ApiThirdPartyExecuteHistorys]'') AND type in (N''U''))
BEGIN
    IF EXISTS (SELECT * 
               FROM sys.columns 
               WHERE object_id = OBJECT_ID(N''[dbo].[H_ApiThirdPartyExecuteHistorys]'') 
                 AND name = ''StepId'' 
                 AND system_type_id = 231
                 AND max_length = -1)
    BEGIN
        ALTER TABLE [dbo].[H_ApiThirdPartyExecuteHistorys]
        ALTER COLUMN [StepId] NVARCHAR(60);
        PRINT ''Column StepId in table H_ApiThirdPartyExecuteHistorys has been changed to NVARCHAR(60).'';
    END
    ELSE
    BEGIN
        PRINT ''No change needed: StepId column does not exist or is not of type NVARCHAR(MAX).'';
    END
END
ELSE
BEGIN
    PRINT ''Table H_ApiThirdPartyExecuteHistorys does not exist.'';
END

-- Create column Options in table DatabaseConnections
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[DatabaseConnections]'') AND type in (N''U''))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[DatabaseConnections]'') AND name = ''Options'')
    BEGIN
        ALTER TABLE DatabaseConnections ADD Options NVARCHAR(MAX);
        PRINT ''Column Options added successfully.'';
    END

    PRINT ''Update table DatabaseConnections success.'';
END
ELSE
BEGIN
    PRINT ''Table DatabaseConnections does not exist.'';
END

-- Create CreatedById column in table WF_Defines
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[WF_Defines]'') AND type in (N''U''))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[WF_Defines]'') AND name = ''CreatedById'')
    BEGIN
        ALTER TABLE WF_Defines ADD CreatedById NVARCHAR(60);
        PRINT ''Column CreatedById added successfully.'';
    END

    PRINT ''Update table WF_Defines success.'';
END
ELSE
BEGIN
    PRINT ''Table WF_Defines does not exist.'';
END

-- Create CreatedById column in table RestRequestForGadgetTools
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[RestRequestForGadgetTools]'') AND type in (N''U''))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[RestRequestForGadgetTools]'') AND name = ''CreatedById'')
    BEGIN
        ALTER TABLE RestRequestForGadgetTools ADD CreatedById NVARCHAR(60);
        PRINT ''Column CreatedById added successfully.'';
    END

    PRINT ''Update table RestRequestForGadgetTools success.'';
END
ELSE
BEGIN
    PRINT ''Table RestRequestForGadgetTools does not exist.'';
END

-- Create table PB_Pages
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[PB_Pages]'') AND type in (N''U''))
BEGIN
    CREATE TABLE [dbo].[PB_Pages](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[IsDeploy] [bit] NOT NULL,
	[IsLock] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
	CONSTRAINT [PK_PB_Pages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT ''Create table PB_Pages success.'';
END

-- Create table PB_Layouts
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[PB_Layouts]'') AND type in (N''U''))
BEGIN
    CREATE TABLE [dbo].[PB_Layouts](
	[Id] [nvarchar](60) NOT NULL,
	[Name] [nvarchar](450) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
 CONSTRAINT [PK_PB_Layouts] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT ''Create table PB_Layouts success.'';
END

-- Create table PB_ComponentInLayouts
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[PB_ComponentInLayouts]'') AND type in (N''U''))
BEGIN
	CREATE TABLE [dbo].[PB_ComponentInLayouts](
		[Id] [nvarchar](60) NOT NULL,
		[Name] [nvarchar](450) NULL,
		[LayoutId] [nvarchar](60) NULL,
		[Type] [int] NULL,
		[Description] [nvarchar](max) NULL,
		[FEConfig] [nvarchar](max) NULL,
		[BEConfig] [nvarchar](max) NULL,
		[PositionX] [real] NULL,
		[PositionY] [real] NULL,
		[MinWidth] [int] NULL,
		[MinHeight] [int] NULL,
		[IsDelete] [bit] NULL,
		[CreatedDate] [datetime2](7) NULL,
		[ModificationDate] [datetime2](7) NULL
	 CONSTRAINT [PK_PB_ComponentInLayouts] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT ''Create table PB_ComponentInLayouts success.'';
END

-- Create table PB_SessionSharePageConfigs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[PB_SessionSharePageConfigs]'') AND type in (N''U''))
BEGIN
    CREATE TABLE [dbo].[PB_SessionSharePageConfigs](
	[Id] [nvarchar](60) NOT NULL,
	[Email] [nvarchar](450) NOT NULL,
	[PageId] [nvarchar](60) NOT NULL,
	[AccessKey] [nvarchar](60) NOT NULL,
	[ExpiresToken] [datetime2](7) NULL,
	[SendDate] [datetime2](7) NULL,
	[Message] [nvarchar](max) NOT NULL,
	[Status] [int] NOT NULL,
	[IsAccessKeyValid] [bit] NOT NULL,
	[CreatedDate] [datetime2](7) NULL,
	[IsDelete] [bit] NOT NULL,
	[ModificationDate] [datetime2](7) NULL,
 CONSTRAINT [PK_PB_SessionSharePageConfigs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

	PRINT ''Create table PB_SessionSharePageConfigs success.'';
END

-- Create index in table H_ApiThirdPartyExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_ApiThirdPartyExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_ApiThirdPartyExecuteHistorys_Wfid_CreatedDate_KeyExecuteRunApiThirdParty_StatusCode_StepId_SiteRun_Url_StepName'' AND object_id = OBJECT_ID(''H_ApiThirdPartyExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_ApiThirdPartyExecuteHistorys_Wfid_CreatedDate_KeyExecuteRunApiThirdParty_StatusCode_StepId_SiteRun_Url_StepName ON [dbo].[H_ApiThirdPartyExecuteHistorys]
		(
			[Wfid] ASC,
			[CreatedDate] ASC,
			[KeyExecuteRunApiThirdParty] ASC,
			[StatusCode] ASC,
			[StepId] ASC,
			[SiteRun] ASC,
			[Url] ASC,
			[StepName] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
		PRINT ''Create index in table H_ApiThirdPartyExecuteHistorys success.'';
	END

	PRINT ''index IX_H_ApiThirdPartyExecuteHistorys_Wfid_CreatedDate_KeyExecuteRunApiThirdParty_StatusCode_StepId_SiteRun_Url_StepName in table H_ApiThirdPartyExecuteHistorys existed.'';
END

-- Create index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate in table H_ApiThirdPartyExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_ApiThirdPartyExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_ApiThirdPartyExecuteHistorys_CreatedDate'' AND object_id = OBJECT_ID(''H_ApiThirdPartyExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_ApiThirdPartyExecuteHistorys_CreatedDate ON [dbo].[H_ApiThirdPartyExecuteHistorys] (CreatedDate);
		PRINT ''Create index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate in table H_ApiThirdPartyExecuteHistorys success.'';
	END

	PRINT ''index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate in table H_ApiThirdPartyExecuteHistorys existed.'';
END

-- Create index IX_H_ApiThirdPartyExecuteHistorys_Status in table H_ApiThirdPartyExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_ApiThirdPartyExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_ApiThirdPartyExecuteHistorys_Status'' AND object_id = OBJECT_ID(''H_ApiThirdPartyExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_ApiThirdPartyExecuteHistorys_Status ON [dbo].[H_ApiThirdPartyExecuteHistorys] (Status);

		PRINT ''Create index IX_H_ApiThirdPartyExecuteHistorys_Status in table H_ApiThirdPartyExecuteHistorys success.'';
	END

	PRINT ''index IX_H_ApiThirdPartyExecuteHistorys_Status in table H_ApiThirdPartyExecuteHistorys existed.'';
END

-- Create index IX_H_ApiThirdPartyExecuteHistorys_SiteRun in table H_ApiThirdPartyExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_ApiThirdPartyExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_ApiThirdPartyExecuteHistorys_SiteRun'' AND object_id = OBJECT_ID(''H_ApiThirdPartyExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_ApiThirdPartyExecuteHistorys_SiteRun ON [dbo].[H_ApiThirdPartyExecuteHistorys] (SiteRun);

		PRINT ''Create index IX_H_ApiThirdPartyExecuteHistorys_SiteRun in table H_ApiThirdPartyExecuteHistorys success.'';
	END

	PRINT ''index IX_H_ApiThirdPartyExecuteHistorys_SiteRun in table H_ApiThirdPartyExecuteHistorys existed.'';
END

-- Create index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate_Status in table H_ApiThirdPartyExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_ApiThirdPartyExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_ApiThirdPartyExecuteHistorys_CreatedDate_Status'' AND object_id = OBJECT_ID(''H_ApiThirdPartyExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_ApiThirdPartyExecuteHistorys_CreatedDate_Status ON [dbo].[H_ApiThirdPartyExecuteHistorys] (CreatedDate, Status);

		PRINT ''Create index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate_Status in table H_ApiThirdPartyExecuteHistorys success.'';
	END

	PRINT ''index IX_H_ApiThirdPartyExecuteHistorys_CreatedDate_Status in table H_ApiThirdPartyExecuteHistorys existed.'';
END

-- Create index IX_H_WorkflowExecuteHistorys_CreatedDate in table H_WorkflowExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_WorkflowExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_WorkflowExecuteHistorys_CreatedDate'' AND object_id = OBJECT_ID(''H_WorkflowExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_WorkflowExecuteHistorys_CreatedDate ON [dbo].[H_WorkflowExecuteHistorys] (CreatedDate);

		PRINT ''Create index IX_H_WorkflowExecuteHistorys_CreatedDate in table H_WorkflowExecuteHistorys success.'';
	END

	PRINT ''index IX_H_WorkflowExecuteHistorys_CreatedDate in table H_WorkflowExecuteHistorys existed.'';
END

-- Create index IX_H_WorkflowExecuteHistorys_Status in table H_WorkflowExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_WorkflowExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_WorkflowExecuteHistorys_Status'' AND object_id = OBJECT_ID(''H_WorkflowExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_WorkflowExecuteHistorys_Status ON [dbo].[H_WorkflowExecuteHistorys] (Status);
		
		PRINT ''Create index IX_H_WorkflowExecuteHistorys_Status in table H_WorkflowExecuteHistorys success.'';
	END

	PRINT ''index IX_H_WorkflowExecuteHistorys_Status in table H_WorkflowExecuteHistorys existed.'';
END

-- Create index IX_H_WorkflowExecuteHistorys_SiteRun in table H_WorkflowExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_WorkflowExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_WorkflowExecuteHistorys_SiteRun'' AND object_id = OBJECT_ID(''H_WorkflowExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_WorkflowExecuteHistorys_SiteRun ON [dbo].[H_WorkflowExecuteHistorys] (SiteRun);

		PRINT ''Create index IX_H_WorkflowExecuteHistorys_SiteRun in table H_WorkflowExecuteHistorys success.'';
	END

	PRINT ''index IX_H_WorkflowExecuteHistorys_SiteRun in table H_WorkflowExecuteHistorys existed.'';
END

-- Create index IX_H_WorkflowExecuteHistorys_CreatedDate_Status in table H_WorkflowExecuteHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''H_WorkflowExecuteHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_H_WorkflowExecuteHistorys_CreatedDate_Status'' AND object_id = OBJECT_ID(''H_WorkflowExecuteHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_H_WorkflowExecuteHistorys_CreatedDate_Status ON [dbo].[H_WorkflowExecuteHistorys] (CreatedDate, Status);

		PRINT ''Create index IX_H_WorkflowExecuteHistorys_CreatedDate_Status in table H_WorkflowExecuteHistorys success.'';
	END

	PRINT ''index IX_H_WorkflowExecuteHistorys_CreatedDate_Status in table H_WorkflowExecuteHistorys existed.'';
END

-- Create index IX_S_ImplementationHistorys_CreatedDate in table S_ImplementationHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''S_ImplementationHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_S_ImplementationHistorys_CreatedDate'' AND object_id = OBJECT_ID(''S_ImplementationHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_S_ImplementationHistorys_CreatedDate ON [dbo].[S_ImplementationHistorys] (CreatedDate);

		PRINT ''Create index IX_S_ImplementationHistorys_CreatedDate in table S_ImplementationHistorys success.'';
	END

	PRINT ''index IX_S_ImplementationHistorys_CreatedDate in table S_ImplementationHistorys existed.'';
END

-- Create index IX_S_ImplementationHistorys_StatusPlan in table S_ImplementationHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''S_ImplementationHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_S_ImplementationHistorys_StatusPlan'' AND object_id = OBJECT_ID(''S_ImplementationHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_S_ImplementationHistorys_StatusPlan ON [dbo].[S_ImplementationHistorys] (StatusPlan);

		PRINT ''Create index IX_S_ImplementationHistorys_StatusPlan in table S_ImplementationHistorys success.'';
	END

	PRINT ''index IX_S_ImplementationHistorys_StatusPlan in table S_ImplementationHistorys existed.'';
END

-- Create index IX_S_ImplementationHistorys_Site in table S_ImplementationHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''S_ImplementationHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_S_ImplementationHistorys_Site'' AND object_id = OBJECT_ID(''S_ImplementationHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_S_ImplementationHistorys_Site ON [dbo].[S_ImplementationHistorys] (Site);

		PRINT ''Create index IX_S_ImplementationHistorys_Site in table S_ImplementationHistorys success.'';
	END

	PRINT ''index IX_S_ImplementationHistorys_Site in table S_ImplementationHistorys existed.'';
END

-- Create index IX_S_ImplementationHistorys_CreatedDate_StatusPlan in table S_ImplementationHistorys
IF EXISTS (SELECT * FROM sys.tables WHERE name = ''S_ImplementationHistorys'')
BEGIN
    IF NOT EXISTS (
        SELECT * 
        FROM sys.indexes 
        WHERE name = ''IX_S_ImplementationHistorys_CreatedDate_StatusPlan'' AND object_id = OBJECT_ID(''S_ImplementationHistorys'')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_S_ImplementationHistorys_CreatedDate_StatusPlan ON [dbo].[S_ImplementationHistorys] (CreatedDate, StatusPlan);

		PRINT ''Create index IX_S_ImplementationHistorys_CreatedDate_StatusPlan in table S_ImplementationHistorys success.'';
	END

	PRINT ''index IX_S_ImplementationHistorys_CreatedDate_StatusPlan in table S_ImplementationHistorys existed.'';
END';

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
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

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N''Options'' 
    AND Object_ID = Object_ID(N''DatabaseConnections''))
BEGIN
    ALTER TABLE DatabaseConnections
    ADD Options nvarchar(max) null
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N''ListTLSType'' 
    AND Object_ID = Object_ID(N''RestRequestForGadgetTools''))
BEGIN
    ALTER TABLE RestRequestForGadgetTools
    ADD ListTLSType nvarchar(max) null
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N''H_ApiThirdPartyExecuteHistorys'')
AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N''H_ApiThirdPartyExcuteHistorys'')
BEGIN
    EXEC sp_rename ''H_ApiThirdPartyExcuteHistorys'', ''H_ApiThirdPartyExecuteHistorys'';
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N''H_WorkflowExecuteHistorys'')
AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N''H_WorkflowExcuteHistorys'')
BEGIN
exec sp_rename ''H_WorkflowExcuteHistorys'', ''H_WorkflowExecuteHistorys'';
End;

IF Not EXISTS (SELECT 1 FROM sys.tables WHERE name = N''CB_ConfigChats'')
BEGIN
    CREATE TABLE [dbo].[CB_ConfigChats](
	[Id] [nvarchar](256) NOT NULL,
	[TokenBOTSendChatBot] [nvarchar](max) NULL,
	[IC_Token] [nvarchar](max) NULL,
	[ListToken] [nvarchar](max) NULL,
	[Url_Api_BOT_UploadFile] [nvarchar](256) NULL,
	[Url_Api_BOT_SendMessage] [nvarchar](256) NULL,
	[Url_Api_BOT_DisableBot] [nvarchar](256) NULL,
	[Url_Api_BOT_EnableBot] [nvarchar](256) NULL,
	[IdBOT] [nvarchar](256) NULL,
	[tenant_id] [nvarchar](256) NULL,
	[TypeChat] [int] NULL,
	[EndChatTime] [nvarchar](256) NULL,
	[UrlShowChatIBox] [nvarchar](256) NULL,
	[IsDelete] [bit] NULL,
	[CreatedDate] [datetime] NULL,
	[ModificationDate] [datetime] NULL,
	[WF_UpdateInteractionCRM] [nvarchar](256) NULL,
	[WF_PutAllChatIC] [nvarchar](256) NULL,
    [WF_UpdateIC_Interaction] [nvarchar](256) NULL,
    [WF_SendChatIC] [nvarchar](256) NULL,
	[WF_CustomerIdentification] [nvarchar](256) NULL,
    [WF_InteractionCRM] [nvarchar](256) NULL,
	[FileDay] [nvarchar](256) NULL,
	[PathBackUp] [nvarchar](256) NULL,
	[PathRestore] [nvarchar](256) NULL,
	[UserChatBot] [nvarchar](256) NULL,
	[PassChatBot] [nvarchar](256) NULL,
	[Retry] [nvarchar](256) NULL,
	[Ev_User_Support] [nvarchar](256) NULL,
	[Ev_Predict] [nvarchar](256) NULL,
	[Ev_EndChat] [nvarchar](256) NULL,
    [Ev_Tag] [nvarchar](256) NULL,
	[Url_IC] [nvarchar](256) NULL,
 CONSTRAINT [PK_CB_ConfigChats] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END;

IF Not EXISTS (SELECT 1 FROM sys.tables WHERE name = N''CB_ZipFileHistories'')
BEGIN
CREATE TABLE [dbo].[CB_ZipFileHistories](
	[Id] [nvarchar](256) NOT NULL,
	[CreatedDate] [datetime] NULL,
	[IsDelete] [bit] NULL,
	[ModificationDate] [datetime] NULL,
	[NameFile] [nvarchar](256) NULL,
	[PathFile] [nvarchar](256) NULL,
	[ListFileZip] [nvarchar](256) NULL,
	[ThisSite] [nvarchar](256) NULL,
 CONSTRAINT [PK_CB_ZipFileHistories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
End;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID(N''dbo.CB_ConfigChats'') 
    AND name = N''DF_CB_ConfigChats_Id''
)
BEGIN
ALTER TABLE [dbo].[CB_ConfigChats] ADD  CONSTRAINT [DF_CB_ConfigChats_Id]  DEFAULT (newid()) FOR [Id]
End;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID(N''dbo.CB_ZipFileHistories'') 
    AND name = N''DF_CB_ZipFileHistories_Id''
)
Begin
ALTER TABLE [dbo].[CB_ZipFileHistories] ADD  CONSTRAINT [DF_CB_ZipFileHistories_Id]  DEFAULT (newid()) FOR [Id]
End;'


EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
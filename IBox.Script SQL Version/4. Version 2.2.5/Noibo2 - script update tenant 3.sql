
DECLARE @dbName NVARCHAR(255);
DECLARE @sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR
SELECT name 
FROM sys.databases 
WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'IBOX_MA05', 'IBOX_MA07', 'IBOX_MA20');
--WHERE name IN ('TTQ2_133572085911576710');
OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
set @sql = 'USE [' + @dbName + ']; 
IF not EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
           WHERE TABLE_NAME = ''CB_Customers'' AND TABLE_SCHEMA = ''dbo'') BEGIN
CREATE TABLE [dbo].[CB_Customers](
	[SenderId] [nvarchar](256) NOT NULL,
	[CustomerInfo] [nvarchar](256) NULL,
	[cif_list_data] [nvarchar](256) NULL,
	[Phone] [nvarchar](256) NULL,
	[ContactId] [nvarchar](256) NULL,
	[CreatedDate] [datetime] NULL,
	[ModificationDate] [datetime] NULL,
	[IsDelete] [bit] NULL,
	[Id] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_CB_Customers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
End;

IF not EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
           WHERE TABLE_NAME = ''CB_ChatStorages'' AND TABLE_SCHEMA = ''dbo'') 
BEGIN
CREATE TABLE [dbo].[CB_ChatStorages](
	[Id] [nvarchar](250) NOT NULL,
	[SenderId] [nvarchar](250) NULL,
	[StoragePath] [nvarchar](250) NULL,
	[CreatedDate] [datetime] NULL,
	[IsDelete] [bit] NULL,
	[ModificationDate] [datetime] NULL,
 CONSTRAINT [PK_CB_ChatStorages] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
END;


IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CreatedDate_Phone'' 
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CreatedDate_Phone ON dbo.CB_Customers (CreatedDate, Phone);
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CreatedDate_Phone_ContactId''
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CreatedDate_Phone_ContactId ON dbo.CB_Customers (CreatedDate, Phone, ContactId);
END;




IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(''dbo.CB_ChatStorages'')
    AND name = ''DF_CB_ChatStorages_CreatedDate''
)
begin
ALTER TABLE [dbo].[CB_ChatStorages] ADD  CONSTRAINT [DF_CB_ChatStorages_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
end;
IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(''dbo.CB_Customers'')
    AND name = ''DF_CB_Customer_CreatedDate''
)
begin
ALTER TABLE [dbo].[CB_Customers] ADD  CONSTRAINT [DF_CB_Customer_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
end;
'

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
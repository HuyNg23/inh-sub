
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

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_Customers_ContactId'' 
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CB_Customers_ContactId ON dbo.CB_Customers (ContactId);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_Customers_ContactId_CreatedDate'' 
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CB_Customers_ContactId_CreatedDate ON dbo.CB_Customers (CreatedDate, ContactId);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_Customers_SenderId_CreatedDate'' 
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CB_Customers_SenderId_CreatedDate ON dbo.CB_Customers (CreatedDate, SenderId);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE 
        i.is_unique = 1 
        AND i.object_id = OBJECT_ID(''CB_Customers'')
        AND c.name = ''SenderId''
)
BEGIN
    ALTER TABLE CB_Customers
    ADD CONSTRAINT UQ_SenderId UNIQUE (SenderId);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_Customers_Phone'' 
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CB_Customers_Phone ON dbo.CB_Customers (Phone);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_Customers_CreatedDate''
    AND object_id = OBJECT_ID(''dbo.CB_Customers'')
)
BEGIN
    CREATE INDEX IX_CB_Customers_CreatedDate ON dbo.CB_Customers (CreatedDate);
END;
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = ''IX_CB_ChatStorages_CreatedDate'' 
    AND object_id = OBJECT_ID(''dbo.CB_ChatStorages'')
)
BEGIN
    CREATE INDEX IX_CB_ChatStorages_CreatedDate ON dbo.CB_ChatStorages (CreatedDate);
END;'

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
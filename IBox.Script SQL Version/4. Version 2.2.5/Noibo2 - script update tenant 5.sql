
DECLARE @dbName NVARCHAR(255);
DECLARE @sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR
SELECT name 
FROM sys.databases 
WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'IBOX_MA05', 'IBOX_MA07', 'IBOX_MA20');
OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
set @sql = 'USE [' + @dbName + ']; 

DECLARE @sql1 NVARCHAR(MAX);

IF NOT EXISTS (
     SELECT * FROM sys.views WHERE OBJECT_ID = OBJECT_ID(''dbo.VW_CB_Customers'')
)
BEGIN
 set @sql1 = ''
CREATE VIEW VW_CB_Customers
WITH SCHEMABINDING
AS
SELECT [SenderId]
      ,[CustomerInfo]
      ,[cif_list_data]
      ,[Phone]
      ,[ContactId]
      ,[CreatedDate]
      ,[ModificationDate]
      ,[IsDelete]
      ,[Id]
  FROM [dbo].[CB_Customers];
  ''
  exec sp_executesql @sql1
  end
  
 DECLARE @index NVARCHAR(MAX);
IF OBJECT_ID(''dbo.VW_CB_Customers'', ''U'') IS NOT NULL
BEGIN
    DROP TABLE dbo.VW_CB_Customers;
END;


				IF NOT EXISTS (
                                    SELECT 1
                                    FROM sys.indexes
                                    WHERE name = ''IX_VW_CB_Customers_SenderId''
                                          AND object_id = OBJECT_ID(''VW_CB_Customers'')
                                )
                BEGIN
				 set @index = ''CREATE UNIQUE CLUSTERED INDEX IX_VW_CB_Customers_SenderId
                    ON VW_CB_Customers (SenderId);''
				  exec sp_executesql @index;
                END;

			IF NOT EXISTS (
             SELECT 1
             FROM sys.indexes
             WHERE name = ''IX_VW_CB_Customers_CreatedDate''
             AND object_id = OBJECT_ID(''VW_CB_Customers''))
                BEGIN
				 set @index = ''CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_CreatedDate
                    ON VW_CB_Customers (CreatedDate);''
				  exec sp_executesql @index;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = ''IX_VW_CB_Customers_ContactId''
                          AND object_id = OBJECT_ID(''VW_CB_Customers'')
                )
                BEGIN
                  set @index =  ''CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_ContactId
                    ON VW_CB_Customers (ContactId);''
				  exec sp_executesql @index;
                END;
                
               

                 IF NOT EXISTS (
                                    SELECT 1
                                    FROM sys.indexes
                                    WHERE name = ''IX_VW_CB_Customers_Phone''
                                          AND object_id = OBJECT_ID(''VW_CB_Customers'')
                                )
                BEGIN
				 set @index = ''CREATE NONCLUSTERED INDEX  IX_VW_CB_Customers_Phone
                    ON VW_CB_Customers (Phone);''
				  exec sp_executesql @index;
                END;

				   IF NOT EXISTS (
                                    SELECT 1
                                    FROM sys.indexes
                                    WHERE name = ''IX_VW_CB_Customers_CreatedDate_SenderId''
                                          AND object_id = OBJECT_ID(''VW_CB_Customers'')
                                )
                BEGIN
				 set @index = ''CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_CreatedDate_SenderId
                    ON VW_CB_Customers (CreatedDate, SenderId);''
				  exec sp_executesql @index;
                END;

				
				   IF NOT EXISTS (
                                    SELECT 1
                                    FROM sys.indexes
                                    WHERE name = ''IX_VW_CB_Customers_CreatedDate_ContactId''
                                          AND object_id = OBJECT_ID(''VW_CB_Customers'')
                                )
                BEGIN
				 set @index = ''CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_CreatedDate_ContactId
                    ON VW_CB_Customers (CreatedDate, ContactId);''
				  exec sp_executesql @index;
                END;

  '
EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;



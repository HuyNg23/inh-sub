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
		  IF not EXISTS (
			SELECT 1 
			FROM INFORMATION_SCHEMA.COLUMNS 
			WHERE TABLE_NAME = ''CB_ConfigChats''
			AND COLUMN_NAME = ''WF_UpdateIC_Interaction''
		)
		BEGIN
		  ALTER TABLE CB_ConfigChats
		  ADD WF_UpdateIC_Interaction nvarchar(256);
		End

		  IF not EXISTS (
			SELECT 1 
			FROM INFORMATION_SCHEMA.COLUMNS 
			WHERE TABLE_NAME = ''CB_ConfigChats'' 
			AND COLUMN_NAME = ''Ev_Tag''
		)
		BEGIN
		  ALTER TABLE CB_ConfigChats
		  ADD Ev_Tag nvarchar(256);
		End

		  Update CB_ConfigChats set WF_UpdateIC_Interaction = ''
		  Update CB_ConfigChats set Ev_Tag = ''';

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
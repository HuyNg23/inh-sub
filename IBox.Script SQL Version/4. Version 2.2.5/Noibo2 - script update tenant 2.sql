DECLARE @dbName NVARCHAR(255);
DECLARE @sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR
SELECT name 
FROM sys.databases 
WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'IBOX_MA01', 'IBOX_MA06', 'IBOX_MA05', 'IBOX_MA07', 'IBOX_MA20');

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
set @sql = 'USE [' + @dbName + ']; 
		  -- Create column Options in table DatabaseConnections
			IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[DatabaseConnections]'') AND type in (N''U''))
			BEGIN
				IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[DatabaseConnections]'') AND name = ''Options'')
				BEGIN
					UPDATE DatabaseConnections
					SET Options = ''''
					WHERE Options IS NULL;

					PRINT ''Column Options updated successfully.'';
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
				IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[WF_Defines]'') AND name = ''CreatedById'')
				BEGIN
					UPDATE WF_Defines
					SET CreatedById = ''''
					WHERE CreatedById IS NULL;
					PRINT ''Column CreatedById updated successfully.'';
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
				IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[RestRequestForGadgetTools]'') AND name = ''CreatedById'')
				BEGIN
					UPDATE RestRequestForGadgetTools
					SET CreatedById = ''''
					WHERE CreatedById IS NULL;
					PRINT ''Column CreatedById updated successfully.'';
				END

				PRINT ''Update table RestRequestForGadgetTools success.'';
			END
			ELSE
			BEGIN
				PRINT ''Table RestRequestForGadgetTools does not exist.'';
			END';

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
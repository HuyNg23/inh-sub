DECLARE @dbName NVARCHAR(255);
DECLARE @sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR
SELECT name 
FROM sys.databases 
WHERE name IN ('IBOX_MA01', 'IBOX_MA05', 'IBOX_MA06', 'IBOX_MA07', 'IBOX_MA20');

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
set @sql = 'USE [' + @dbName + '];
		    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[U_Roles]'') AND type in (N''U''))
			BEGIN
				IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N''[dbo].[U_Roles]'') AND name = ''Name'')
				BEGIN
					ALTER TABLE U_Roles
					ALTER COLUMN Name NVARCHAR(450) NULL;
					PRINT ''Column Name update successfully.'';
				END

				UPDATE U_Roles
				SET TenantId = '''', PermissionsKey = ''''
				WHERE TenantId IS NULL OR PermissionsKey IS NULL;

				PRINT ''Columns TenantId and PermissionsKey updated successfully.'';

				PRINT ''Update table U_Roles success.'';
			END
			ELSE
			BEGIN
				PRINT ''Table U_Roles does not exist.'';
			END';

EXEC sp_executesql @sql;
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
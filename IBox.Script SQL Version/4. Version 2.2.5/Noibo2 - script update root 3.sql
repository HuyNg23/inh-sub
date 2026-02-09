DECLARE @sql NVARCHAR(MAX) = N'UPDATE T_Tenants SET ';

DECLARE @columns NVARCHAR(MAX) = N'';

SELECT @columns += 
    CASE 
        WHEN @columns = N'' THEN COLUMN_NAME + ' = COALESCE(' + COLUMN_NAME + ', '''')'
        ELSE ', ' + COLUMN_NAME + ' = COALESCE(' + COLUMN_NAME + ', '''')'
    END
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'T_Tenants';

SET @sql += @columns;

EXEC sp_executesql @sql;
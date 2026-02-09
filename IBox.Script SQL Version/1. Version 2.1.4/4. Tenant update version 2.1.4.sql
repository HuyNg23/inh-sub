IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatabaseConnections]') AND type in (N'U'))
BEGIN
	UPDATE [dbo].[DatabaseConnections]
	SET 
		SQLType = ISNULL(SQLType, 0);
		
	PRINT 'Update table DatabaseConnections success.'
END
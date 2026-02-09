-- Add column table H_ApiThirdPartyExcuteHistorys
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[H_ApiThirdPartyExcuteHistorys]') AND type in (N'U'))
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'H_ApiThirdPartyExcuteHistorys') 
           AND name = 'StepName')
	BEGIN
		ALTER TABLE [dbo].[H_ApiThirdPartyExcuteHistorys]
		ADD [StepName] nvarchar(256);
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'H_ApiThirdPartyExcuteHistorys') 
           AND name = 'StepId')
	BEGIN
		ALTER TABLE [dbo].[H_ApiThirdPartyExcuteHistorys]
		ADD [StepId] nvarchar(256);
	END
	PRINT 'Update table H_ApiThirdPartyExcuteHistorys success.'
END

-- Add column table DatabaseConnections
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DatabaseConnections]') AND type in (N'U'))
BEGIN
	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'DatabaseConnections') 
           AND name = 'Port')
	BEGIN
		ALTER TABLE [dbo].[DatabaseConnections]
		ADD Port nvarchar(MAX);
	END

	IF NOT EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'DatabaseConnections') 
           AND name = 'SQLType')
	BEGIN
		ALTER TABLE [dbo].[DatabaseConnections]
		ADD [SQLType] int DEFAULT 0;
	END
	PRINT 'Update table DatabaseConnections success.'
END
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P_Package_Details]') AND type in (N'U'))
BEGIN
	UPDATE [dbo].[P_Package_Details]
	SET 
		Name = ISNULL(Name, ''),
		[Path] = ISNULL([Path], ''),
		[Version] = ISNULL([Version], '');
	 PRINT 'Update table P_Package_Details success.'
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[P_Packages]') AND type in (N'U'))
BEGIN
	UPDATE [dbo].P_Packages
	SET 
		[Name] = ISNULL([Name], ''),
		[Description] = ISNULL([Description], '');
	PRINT 'Update table P_Packages success.'
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Tenants]') AND type in (N'U'))
BEGIN
	UPDATE [dbo].T_Tenants
	SET 
		[Smtp_Host] = ISNULL([Smtp_Host], ''),
		[Smtp_Port] = ISNULL([Smtp_Port], ''),
		[Smtp_Username] = ISNULL([Smtp_Username], ''),
		[Smtp_Password] = ISNULL([Smtp_Password], ''),
		[Smtp_Credentials] = ISNULL([Smtp_Credentials], 0),
		[Smtp_EnableSSL] = ISNULL([Smtp_EnableSSL], 0),
		[MailTo] = ISNULL([MailTo], ''),
		[MailCC] = ISNULL([MailCC], ''),
		[MailBCC] = ISNULL([MailBCC], ''),
		[MailTitle] = ISNULL([MailTitle], ''),
		[MailBody] = ISNULL([MailBody], ''),
		[AutoSendMail] = ISNULL([AutoSendMail], 0);
	PRINT 'Update table T_Tenants success.'
END
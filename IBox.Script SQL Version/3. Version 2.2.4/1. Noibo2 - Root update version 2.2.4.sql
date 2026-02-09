IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'Catalog' 
    AND Object_ID = Object_ID(N'D_DatabaseConnections'))
BEGIN
    ALTER TABLE D_DatabaseConnections
    ADD Catalog nvarchar(max) null
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE Name = N'Port' 
    AND Object_ID = Object_ID(N'D_DatabaseConnections'))
BEGIN
    ALTER TABLE D_DatabaseConnections
    ADD Port nvarchar(max) null
END;

IF NOT EXISTS (
    SELECT 1 
    FROM sys.default_constraints 
    WHERE parent_object_id = OBJECT_ID(N'dbo.C_ConfigRoot') 
    AND name = N'DF_C_ConfigRoot_Id'
)
Begin
ALTER TABLE [dbo].[C_ConfigRoot] ADD  CONSTRAINT [DF_C_ConfigRoot_Id]  DEFAULT (newid()) FOR [Id]
End;


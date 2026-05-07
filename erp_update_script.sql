BEGIN TRANSACTION;
GO

ALTER TABLE [TransactionLedgers] DROP CONSTRAINT [FK_TransactionLedgers_AspNetUsers_AgentUserId];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TransactionLedgers]') AND [c].[name] = N'ReferenceNumber');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [TransactionLedgers] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [TransactionLedgers] ALTER COLUMN [ReferenceNumber] nvarchar(max) NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TransactionLedgers]') AND [c].[name] = N'BuyerName');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TransactionLedgers] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [TransactionLedgers] ALTER COLUMN [BuyerName] nvarchar(max) NULL;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TransactionLedgers]') AND [c].[name] = N'AgentUserId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [TransactionLedgers] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [TransactionLedgers] ALTER COLUMN [AgentUserId] nvarchar(450) NULL;
GO

ALTER TABLE [TransactionLedgers] ADD [DateCommissionPaid] datetime2 NULL;
GO

ALTER TABLE [TransactionLedgers] ADD [IsCommissionPaid] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SystemLogs]') AND [c].[name] = N'Details');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SystemLogs] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [SystemLogs] ALTER COLUMN [Details] nvarchar(max) NULL;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PropertyStatuses]') AND [c].[name] = N'Status');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [PropertyStatuses] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [PropertyStatuses] ALTER COLUMN [Status] nvarchar(max) NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Properties]') AND [c].[name] = N'Type');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Properties] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Properties] ALTER COLUMN [Type] nvarchar(max) NULL;
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Properties]') AND [c].[name] = N'PropertyName');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Properties] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Properties] ALTER COLUMN [PropertyName] nvarchar(max) NULL;
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Properties]') AND [c].[name] = N'Details');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Properties] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Properties] ALTER COLUMN [Details] nvarchar(max) NULL;
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'Status');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Inquiries] ALTER COLUMN [Status] nvarchar(max) NULL;
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'Reason');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Inquiries] ALTER COLUMN [Reason] nvarchar(max) NULL;
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'PhoneNumber');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Inquiries] ALTER COLUMN [PhoneNumber] nvarchar(max) NULL;
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'FullName');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Inquiries] ALTER COLUMN [FullName] nvarchar(max) NULL;
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inquiries]') AND [c].[name] = N'Email');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Inquiries] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Inquiries] ALTER COLUMN [Email] nvarchar(max) NULL;
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Clients]') AND [c].[name] = N'Status');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Clients] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Clients] ALTER COLUMN [Status] nvarchar(max) NULL;
GO

CREATE TABLE [CompanyExpenses] (
    [Id] int NOT NULL IDENTITY,
    [Category] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DateIncurred] datetime2 NOT NULL,
    [LoggedByUserId] nvarchar(450) NULL,
    CONSTRAINT [PK_CompanyExpenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanyExpenses_AspNetUsers_LoggedByUserId] FOREIGN KEY ([LoggedByUserId]) REFERENCES [AspNetUsers] ([Id])
);
GO

CREATE INDEX [IX_CompanyExpenses_LoggedByUserId] ON [CompanyExpenses] ([LoggedByUserId]);
GO

ALTER TABLE [TransactionLedgers] ADD CONSTRAINT [FK_TransactionLedgers_AspNetUsers_AgentUserId] FOREIGN KEY ([AgentUserId]) REFERENCES [AspNetUsers] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422181233_AddExpensesAndPayroll', N'8.0.23');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [CompanyExpenses] DROP CONSTRAINT [FK_CompanyExpenses_AspNetUsers_LoggedByUserId];
GO

DROP INDEX [IX_CompanyExpenses_LoggedByUserId] ON [CompanyExpenses];
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CompanyExpenses]') AND [c].[name] = N'LoggedByUserId');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [CompanyExpenses] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [CompanyExpenses] DROP COLUMN [LoggedByUserId];
GO

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CompanyExpenses]') AND [c].[name] = N'Description');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [CompanyExpenses] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [CompanyExpenses] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CompanyExpenses]') AND [c].[name] = N'Category');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [CompanyExpenses] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [CompanyExpenses] ALTER COLUMN [Category] nvarchar(max) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260422193104_AddERPFeatures', N'8.0.23');
GO

COMMIT;
GO


BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116044416_MigrateIsReceivedToStatus'
)
BEGIN

    UPDATE dbo.OrderItems SET Status = 'Received' WHERE IsReceived = 1 AND Status <> 'Voided';

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116044416_MigrateIsReceivedToStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261116044416_MigrateIsReceivedToStatus', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116044635_DropOrderItemIsReceived'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dbo].[OrderItems]') AND [c].[name] = N'IsReceived');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [dbo].[OrderItems] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [dbo].[OrderItems] DROP COLUMN [IsReceived];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261116044635_DropOrderItemIsReceived'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261116044635_DropOrderItemIsReceived', N'10.0.8');
END;

COMMIT;
GO


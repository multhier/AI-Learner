IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(40) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Role] int NOT NULL,
        [Bio] nvarchar(1000) NULL,
        [IsBanned] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] int NOT NULL IDENTITY,
        [SenderId] int NOT NULL,
        [ReceiverId] int NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Topics] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [Slug] nvarchar(120) NOT NULL,
        [CreatedById] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastEntryAt] datetime2 NOT NULL,
        [EntryCount] int NOT NULL,
        [IsLocked] bit NOT NULL,
        CONSTRAINT [PK_Topics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Topics_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Entries] (
        [Id] int NOT NULL IDENTITY,
        [TopicId] int NOT NULL,
        [AuthorId] int NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [UpVotes] int NOT NULL,
        [DownVotes] int NOT NULL,
        [FavoriteCount] int NOT NULL,
        CONSTRAINT [PK_Entries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Entries_Topics_TopicId] FOREIGN KEY ([TopicId]) REFERENCES [Topics] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Entries_Users_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Favorites] (
        [EntryId] int NOT NULL,
        [UserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Favorites] PRIMARY KEY ([EntryId], [UserId]),
        CONSTRAINT [FK_Favorites_Entries_EntryId] FOREIGN KEY ([EntryId]) REFERENCES [Entries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Reports] (
        [Id] int NOT NULL IDENTITY,
        [EntryId] int NOT NULL,
        [ReporterId] int NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [IsResolved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reports_Entries_EntryId] FOREIGN KEY ([EntryId]) REFERENCES [Entries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reports_Users_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE TABLE [Votes] (
        [EntryId] int NOT NULL,
        [UserId] int NOT NULL,
        [Value] smallint NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Votes] PRIMARY KEY ([EntryId], [UserId]),
        CONSTRAINT [FK_Votes_Entries_EntryId] FOREIGN KEY ([EntryId]) REFERENCES [Entries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Votes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Entries_AuthorId_CreatedAt] ON [Entries] ([AuthorId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Entries_CreatedAt] ON [Entries] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Entries_TopicId_CreatedAt] ON [Entries] ([TopicId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Favorites_UserId] ON [Favorites] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Messages_ReceiverId_IsRead] ON [Messages] ([ReceiverId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_EntryId] ON [Reports] ([EntryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_IsResolved] ON [Reports] ([IsResolved]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_ReporterId] ON [Reports] ([ReporterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Topics_CreatedById] ON [Topics] ([CreatedById]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Topics_LastEntryAt] ON [Topics] ([LastEntryAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Topics_Title] ON [Topics] ([Title]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Votes_UserId] ON [Votes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126075750_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126075750_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO


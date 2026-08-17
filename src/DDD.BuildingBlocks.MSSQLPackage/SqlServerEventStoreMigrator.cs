using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DDD.BuildingBlocks.MSSQLPackage;

public sealed class SqlServerEventStoreMigrator(
    string connectionString,
    SqlServerEventStoreOptions? options = null)
{
    private readonly SqlServerEventStoreOptions _options = options ?? new SqlServerEventStoreOptions();

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var schema = _options.Schema;
        var sql = $$"""
            IF SCHEMA_ID(N'{{schema}}') IS NULL
                EXEC(N'CREATE SCHEMA [{{schema}}]');

            IF OBJECT_ID(N'[{{schema}}].[SchemaVersions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[SchemaVersions]
                (
                    [Version] int NOT NULL CONSTRAINT [PK_SchemaVersions] PRIMARY KEY,
                    [AppliedAt] datetimeoffset(7) NOT NULL CONSTRAINT [DF_SchemaVersions_AppliedAt] DEFAULT SYSDATETIMEOFFSET()
                );
            END;

            IF OBJECT_ID(N'[{{schema}}].[EventStreams]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[EventStreams]
                (
                    [AggregateType] nvarchar(512) NOT NULL,
                    [StreamId] nvarchar(512) NOT NULL,
                    [CurrentVersion] bigint NOT NULL,
                    [CreatedAt] datetimeoffset(7) NOT NULL CONSTRAINT [DF_EventStreams_CreatedAt] DEFAULT SYSDATETIMEOFFSET(),
                    [UpdatedAt] datetimeoffset(7) NOT NULL CONSTRAINT [DF_EventStreams_UpdatedAt] DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT [PK_EventStreams] PRIMARY KEY ([AggregateType], [StreamId]),
                    CONSTRAINT [CK_EventStreams_CurrentVersion] CHECK ([CurrentVersion] >= -1)
                );
            END;

            IF OBJECT_ID(N'[{{schema}}].[EventStoreState]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[EventStoreState]
                (
                    [Singleton] bit NOT NULL CONSTRAINT [PK_EventStoreState] PRIMARY KEY,
                    [NextGlobalPosition] bigint NOT NULL,
                    CONSTRAINT [CK_EventStoreState_Singleton] CHECK ([Singleton] = 1),
                    CONSTRAINT [CK_EventStoreState_Position] CHECK ([NextGlobalPosition] >= 0)
                );
                INSERT INTO [{{schema}}].[EventStoreState] ([Singleton], [NextGlobalPosition]) VALUES (1, 0);
            END;

            IF OBJECT_ID(N'[{{schema}}].[Events]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[Events]
                (
                    [GlobalPosition] bigint NOT NULL CONSTRAINT [PK_Events] PRIMARY KEY,
                    [EventId] uniqueidentifier NOT NULL,
                    [AggregateType] nvarchar(512) NOT NULL,
                    [StreamId] nvarchar(512) NOT NULL,
                    [StreamVersion] bigint NOT NULL,
                    [EventType] nvarchar(512) NOT NULL,
                    [SchemaVersion] int NOT NULL,
                    [OccurredAt] datetimeoffset(7) NOT NULL,
                    [CommittedAt] datetimeoffset(7) NOT NULL CONSTRAINT [DF_Events_CommittedAt] DEFAULT SYSDATETIMEOFFSET(),
                    [CorrelationId] nvarchar(512) NULL,
                    [CausationId] nvarchar(512) NULL,
                    [CommandId] nvarchar(512) NULL,
                    [Actor] nvarchar(512) NULL,
                    [TurnId] nvarchar(512) NULL,
                    [Payload] nvarchar(max) NOT NULL,
                    CONSTRAINT [UQ_Events_EventId] UNIQUE ([EventId]),
                    CONSTRAINT [UQ_Events_StreamVersion] UNIQUE ([AggregateType], [StreamId], [StreamVersion]),
                    CONSTRAINT [FK_Events_EventStreams] FOREIGN KEY ([AggregateType], [StreamId])
                        REFERENCES [{{schema}}].[EventStreams] ([AggregateType], [StreamId]),
                    CONSTRAINT [CK_Events_StreamVersion] CHECK ([StreamVersion] >= 0),
                    CONSTRAINT [CK_Events_SchemaVersion] CHECK ([SchemaVersion] >= 1),
                    CONSTRAINT [CK_Events_PayloadJson] CHECK (ISJSON([Payload]) = 1 AND LEFT(LTRIM([Payload]), 1) = N'{')
                );
                CREATE INDEX [IX_Events_StreamRead]
                    ON [{{schema}}].[Events] ([AggregateType], [StreamId], [StreamVersion])
                    INCLUDE ([GlobalPosition]);
            END;

            IF TYPE_ID(N'[{{schema}}].[EventBatchType]') IS NULL
                EXEC(N'CREATE TYPE [{{schema}}].[EventBatchType] AS TABLE
                (
                    [BatchIndex] int NOT NULL PRIMARY KEY,
                    [EventId] uniqueidentifier NOT NULL,
                    [AggregateType] nvarchar(512) NOT NULL,
                    [StreamId] nvarchar(512) NOT NULL,
                    [StreamVersion] bigint NOT NULL,
                    [EventType] nvarchar(512) NOT NULL,
                    [SchemaVersion] int NOT NULL,
                    [OccurredAt] datetimeoffset(7) NOT NULL,
                    [CorrelationId] nvarchar(512) NULL,
                    [CausationId] nvarchar(512) NULL,
                    [CommandId] nvarchar(512) NULL,
                    [Actor] nvarchar(512) NULL,
                    [TurnId] nvarchar(512) NULL,
                    [Payload] nvarchar(max) NOT NULL
                )');

            IF NOT EXISTS (SELECT 1 FROM [{{schema}}].[SchemaVersions] WHERE [Version] = 1)
                INSERT INTO [{{schema}}].[SchemaVersions] ([Version]) VALUES (1);

            IF OBJECT_ID(N'[{{schema}}].[ProjectionCheckpoints]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[ProjectionCheckpoints]
                (
                    [ProjectionName] nvarchar(512) NOT NULL,
                    [ProjectionVersion] int NOT NULL,
                    [LastProcessedPosition] bigint NOT NULL,
                    [Status] int NOT NULL,
                    [FailedPosition] bigint NULL,
                    [LastError] nvarchar(max) NULL,
                    [UpdatedAt] datetimeoffset(7) NOT NULL CONSTRAINT [DF_ProjectionCheckpoints_UpdatedAt] DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT [PK_ProjectionCheckpoints] PRIMARY KEY ([ProjectionName], [ProjectionVersion]),
                    CONSTRAINT [CK_ProjectionCheckpoints_Version] CHECK ([ProjectionVersion] >= 1),
                    CONSTRAINT [CK_ProjectionCheckpoints_Position] CHECK ([LastProcessedPosition] >= -1)
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [{{schema}}].[SchemaVersions] WHERE [Version] = 2)
                INSERT INTO [{{schema}}].[SchemaVersions] ([Version]) VALUES (2);

            IF OBJECT_ID(N'[{{schema}}].[Snapshots]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{{schema}}].[Snapshots]
                (
                    [AggregateType] nvarchar(512) NOT NULL,
                    [StreamId] nvarchar(512) NOT NULL,
                    [StreamVersion] bigint NOT NULL,
                    [SnapshotType] nvarchar(512) NOT NULL,
                    [SchemaVersion] int NOT NULL,
                    [CreatedAt] datetimeoffset(7) NOT NULL,
                    [Payload] nvarchar(max) NOT NULL,
                    CONSTRAINT [PK_Snapshots] PRIMARY KEY ([AggregateType], [StreamId], [StreamVersion]),
                    CONSTRAINT [CK_Snapshots_StreamVersion] CHECK ([StreamVersion] >= 0),
                    CONSTRAINT [CK_Snapshots_SchemaVersion] CHECK ([SchemaVersion] >= 1),
                    CONSTRAINT [CK_Snapshots_PayloadJson] CHECK (ISJSON([Payload]) = 1 AND LEFT(LTRIM([Payload]), 1) = N'{')
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [{{schema}}].[SchemaVersions] WHERE [Version] = 3)
                INSERT INTO [{{schema}}].[SchemaVersions] ([Version]) VALUES (3);
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new SqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace DDD.BuildingBlocks.PostgreSQLPackage;

public sealed class PostgreSqlEventStoreMigrator(
    NpgsqlDataSource dataSource,
    PostgreSqlEventStoreOptions? options = null)
{
    private readonly PostgreSqlEventStoreOptions _options = options ?? new PostgreSqlEventStoreOptions();

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        var schema = _options.Schema;
        var sql = $$"""
            CREATE SCHEMA IF NOT EXISTS {{schema}};

            CREATE TABLE IF NOT EXISTS {{schema}}.schema_versions
            (
                version integer PRIMARY KEY,
                applied_at timestamp with time zone NOT NULL DEFAULT transaction_timestamp()
            );

            CREATE TABLE IF NOT EXISTS {{schema}}.event_streams
            (
                aggregate_type text NOT NULL,
                stream_id text NOT NULL,
                current_version bigint NOT NULL,
                created_at timestamp with time zone NOT NULL DEFAULT transaction_timestamp(),
                updated_at timestamp with time zone NOT NULL DEFAULT transaction_timestamp(),
                PRIMARY KEY (aggregate_type, stream_id),
                CONSTRAINT event_streams_current_version_check CHECK (current_version >= -1)
            );

            CREATE TABLE IF NOT EXISTS {{schema}}.event_store_state
            (
                singleton boolean PRIMARY KEY DEFAULT TRUE,
                next_global_position bigint NOT NULL,
                CONSTRAINT event_store_state_singleton_check CHECK (singleton),
                CONSTRAINT event_store_state_position_check CHECK (next_global_position >= 0)
            );

            INSERT INTO {{schema}}.event_store_state (singleton, next_global_position)
            VALUES (TRUE, 0)
            ON CONFLICT (singleton) DO NOTHING;

            CREATE TABLE IF NOT EXISTS {{schema}}.events
            (
                global_position bigint PRIMARY KEY,
                event_id uuid NOT NULL UNIQUE,
                aggregate_type text NOT NULL,
                stream_id text NOT NULL,
                stream_version bigint NOT NULL,
                event_type text NOT NULL,
                schema_version integer NOT NULL,
                occurred_at timestamp with time zone NOT NULL,
                committed_at timestamp with time zone NOT NULL DEFAULT transaction_timestamp(),
                correlation_id text NULL,
                causation_id text NULL,
                command_id text NULL,
                actor text NULL,
                turn_id text NULL,
                payload jsonb NOT NULL,
                CONSTRAINT events_stream_fk FOREIGN KEY (aggregate_type, stream_id)
                    REFERENCES {{schema}}.event_streams (aggregate_type, stream_id),
                CONSTRAINT events_stream_version_unique UNIQUE (aggregate_type, stream_id, stream_version),
                CONSTRAINT events_stream_version_check CHECK (stream_version >= 0),
                CONSTRAINT events_schema_version_check CHECK (schema_version >= 1),
                CONSTRAINT events_payload_object_check CHECK (jsonb_typeof(payload) = 'object')
            );

            CREATE INDEX IF NOT EXISTS events_stream_read_idx
                ON {{schema}}.events (aggregate_type, stream_id, stream_version)
                INCLUDE (global_position);

            INSERT INTO {{schema}}.schema_versions (version)
            VALUES (1)
            ON CONFLICT (version) DO NOTHING;

            CREATE TABLE IF NOT EXISTS {{schema}}.projection_checkpoints
            (
                projection_name text NOT NULL,
                projection_version integer NOT NULL,
                last_processed_position bigint NOT NULL,
                status integer NOT NULL,
                failed_position bigint NULL,
                last_error text NULL,
                updated_at timestamp with time zone NOT NULL DEFAULT transaction_timestamp(),
                PRIMARY KEY (projection_name, projection_version),
                CONSTRAINT projection_checkpoints_version_check CHECK (projection_version >= 1),
                CONSTRAINT projection_checkpoints_position_check CHECK (last_processed_position >= -1)
            );

            INSERT INTO {{schema}}.schema_versions (version)
            VALUES (2)
            ON CONFLICT (version) DO NOTHING;

            CREATE TABLE IF NOT EXISTS {{schema}}.snapshots
            (
                aggregate_type text NOT NULL,
                stream_id text NOT NULL,
                stream_version bigint NOT NULL,
                snapshot_type text NOT NULL,
                schema_version integer NOT NULL,
                created_at timestamp with time zone NOT NULL,
                payload jsonb NOT NULL,
                PRIMARY KEY (aggregate_type, stream_id, stream_version),
                CONSTRAINT snapshots_stream_version_check CHECK (stream_version >= 0),
                CONSTRAINT snapshots_schema_version_check CHECK (schema_version >= 1),
                CONSTRAINT snapshots_payload_object_check CHECK (jsonb_typeof(payload) = 'object')
            );

            INSERT INTO {{schema}}.schema_versions (version)
            VALUES (3)
            ON CONFLICT (version) DO NOTHING;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}

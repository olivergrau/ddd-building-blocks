using System;
using System.Linq;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.MSSQLPackage.Provider;

using System.Data;
using System.Data.Common;
using System.Text;
using Core.Exception;
using Core.Persistence.SnapshotSupport;
using Core.Persistence.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public class SnapshotStorageProvider : ISnapshotStorageProvider
{
    private readonly IOptionsMonitor<SnapshotStorageProviderSettings> _settings;
    private static JsonSerializerSettings? _serializerSetting;

    public SnapshotStorageProvider(IOptionsMonitor<SnapshotStorageProviderSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        _settings = settings;
    }

    public int SnapshotFrequency => _settings.CurrentValue.SnapshotFrequency;

    public async Task<Snapshot?> GetSnapshotAsync(string aggregateId)
    {
        await using (var connection = new SqlConnection(_settings.CurrentValue.ConnectionString))
        await using (var command = connection.CreateCommand())
        {
            try
            {
                await connection.OpenAsync();

                command.CommandText =
                    "SELECT TOP 1 S.* FROM dbo.MAPPINGS M INNER JOIN dbo.SNAPSHOTS S ON M.AGGREGATEID = S.AGGREGATEID WHERE M.[KEY] = @key ORDER BY version DESC";

                command.Parameters.Add(aggregateId.ToSqlParameter("@key"));

                var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ReconstituteSnapshot(reader, out var snapshot);

                    return snapshot;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new ProviderException("Failure reading snapshots from storage.", ex);
            }
        }
    }

    public async Task SaveSnapshotAsync(Snapshot snapshot)
    {
        await using (var connection = new SqlConnection(_settings.CurrentValue.ConnectionString))
        await using (var sqlCommand1 = connection.CreateCommand())
        {
            await connection.OpenAsync();

            var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            sqlCommand1.Transaction = transaction;

            try
            {
                sqlCommand1.CommandText = "SELECT * FROM dbo.MAPPINGS WHERE [KEY] = @key";
                sqlCommand1.Parameters.Add(new SqlParameter("@key", snapshot.SerializedAggregateId));

                var reader = await sqlCommand1.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new Exception($"Mapping for key: {snapshot.SerializedAggregateId} not found.");
                }

                var physicalId = reader.GetGuid(0);

                await reader.CloseAsync();

                var snapshotData =
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(snapshot, GetSerializerSettings()));

                var sqlCommand2 = connection.CreateCommand();
                sqlCommand2.Transaction = transaction;

                sqlCommand2.CommandText = "INSERT INTO dbo.SNAPSHOTS (AGGREGATEID, SERIALIZEDDATA, VERSION, TYPE) VALUES(@id, @data, @version, @type)";

                sqlCommand2.Parameters.Clear();
                sqlCommand2.Parameters.Add(physicalId.ToSqlParameter("@id"));
                sqlCommand2.Parameters.Add(snapshotData.ToSqlParameter("@data"));
                sqlCommand2.Parameters.Add(snapshot.Version.ToSqlParameter("@version"));
                sqlCommand2.Parameters.Add(snapshot.GetType().AssemblyQualifiedName!.ToSqlParameter("@type"));

                var rows = await sqlCommand2.ExecuteNonQueryAsync();

                if (rows <= 0)
                {
                    throw new Exception("Failing inserting snapshot.");
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ProviderException("Failure saving snapshot", ex);
            }
        }
    }

    public async Task<Snapshot?> GetSnapshotAsync(string aggregateId, int version)
    {
        await using (var connection = new SqlConnection(_settings.CurrentValue.ConnectionString))
        await using (var command = connection.CreateCommand())
        {
            try
            {
                await connection.OpenAsync();

                command.CommandText =
                    "SELECT S.* FROM dbo.MAPPINGS M INNER JOIN dbo.SNAPSHOTS S ON M.AGGREGATEID = S.AGGREGATEID WHERE M.[KEY] = @key AND S.version <= @version";

                command.Parameters.Add(aggregateId.ToSqlParameter("@key"));
                command.Parameters.Add(version.ToSqlParameter("@version"));

                var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ReconstituteSnapshot(reader, out var snapshot);

                    return snapshot;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new ProviderException("Failure reading snapshots from storage.", ex);
            }
        }
    }

    private static JsonSerializerSettings GetSerializerSettings()
    {
        return _serializerSetting ??= new JsonSerializerSettings
            { TypeNameHandling = TypeNameHandling.None };
    }

    private static Snapshot DeserializeSnapshotEvent(byte[] eventData, Type? clrType)
    {
        return
#pragma warning disable CS8600
            ((Snapshot)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eventData),
                clrType, GetSerializerSettings()))!;
#pragma warning restore CS8600
    }

    private static void ReconstituteSnapshot(DbDataReader reader, out Snapshot? snapshot)
    {
        //snapshot = null;

        (Guid id, byte[] data, int version, string type) snapshotData = (Guid.Empty, null, 0, "")!;
        snapshotData.id = reader.GetGuid(0);

        snapshotData.version = reader.GetInt32(1);
        snapshotData.type = reader.GetString(2);

        var startIndex = 0;
        const int bufferSize = 255;
        var buffer = new byte[255];

        var retrieval = reader.GetBytes(3, startIndex, buffer, 0, bufferSize);
        snapshotData.data = snapshotData.data.Combine(buffer.Take(Convert.ToInt32(retrieval)).ToArray());

        while (retrieval == bufferSize)
        {
            startIndex += bufferSize;
            retrieval = reader.GetBytes(3, startIndex, buffer, 0, bufferSize);
            snapshotData.data = snapshotData.data.Combine(buffer.Take(Convert.ToInt32(retrieval)).ToArray());
        }

        snapshot = DeserializeSnapshotEvent(snapshotData.data, Type.GetType(snapshotData.type));
    }
}

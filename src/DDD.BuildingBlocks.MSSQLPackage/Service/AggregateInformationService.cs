namespace DDD.BuildingBlocks.MSSQLPackage.Service;

using System;
using System.Threading.Tasks;
using Core.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AggregateInformationService(IOptionsMonitor<AggregateInformationServiceSettings> settings, ILogger<AggregateInformationService> logger)
    : IAggregateInformationService
{
    public async Task<Type?> GetTypeForAggregateId(string aggregateId)
    {
        await using var connection = new SqlConnection(settings.CurrentValue.ConnectionString);
        await using var command = connection.CreateCommand();
        await connection.OpenAsync();

        command.CommandText =
            "SELECT A.TYPE FROM dbo.MAPPINGS M INNER JOIN dbo.AGGREGATES A " +
            "ON M.AGGREGATEID = A.AGGREGATEID " +
            "WHERE M.[KEY] = '" + aggregateId + "'";

        var typeName = await command.ExecuteScalarAsync();

        await connection.CloseAsync();

        if (typeName == null)
        {
            logger.LogDebug("No maching type name in database found. There is possibly no record for the requested aggregate id");
            return null;
        }

        var typeResult = Type.GetType((typeName as string)!);

        if (typeResult == null)
        {
            logger.LogDebug("Found a type name in storage for aggregate id but that type could not be found in the app domain");
        }

        return typeResult;
    }
}

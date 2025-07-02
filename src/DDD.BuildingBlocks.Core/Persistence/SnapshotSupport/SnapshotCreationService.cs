// ReSharper disable TemplateIsNotCompileTimeConstantProblem
namespace DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Repository;

public class SnapshotCreationService(IEventSourcingRepository repository, IAggregateInformationService informationService, ILoggerFactory loggerFactory)
    : ISnapshotCreationService
{
    private readonly ILogger _log = loggerFactory.CreateLogger<SnapshotCreationService>();

    public async Task<Snapshot?> CreateSnapshotFrom(string aggregateId, int version = -1)
    {
        var aggregateType = await informationService.GetTypeForAggregateId(aggregateId);

        if (aggregateType == null)
        {
            _log.LogError("Aggregate type could not be determined for aggregate id: {Id}", aggregateId);
            throw new Exception($"Aggregate type could not be determined for aggregate id: {aggregateId}");
        }

        var aggregate = await repository.GetByIdAsync(aggregateId, aggregateType, version);

        if (aggregate == null)
        {
            throw new Exception($"Aggregate (Type: {aggregateType.FullName}, Id: {aggregateId}, version: {version}) could not be loaded from repository.");
        }

        if (aggregate is not ISnapshotEnabled snapshot)
        {
            _log.LogError($"Aggregate (Type: {aggregateType.FullName}, Id: {aggregateId}) does not implement " + nameof(ISnapshotEnabled));
            return null;
        }

        return snapshot.TakeSnapshot();
    }
}

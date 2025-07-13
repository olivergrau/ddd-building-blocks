using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.ReadModel.Core.Projector.Mission;

public class RocketProjector(IRocketService rocketService, ILogger<RocketProjector> logger)
    :
        ISubscribe<RocketAssigned>,
        ISubscribe<MissionAborted>
{
    private readonly IRocketService _rocketService = rocketService ?? throw new ArgumentNullException(nameof(rocketService));
    private readonly ILogger<RocketProjector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task WhenAsync(RocketAssigned @event)
    {
        var rocket = await _rocketService.GetByIdAsync(@event.RocketId.Value) ?? new Rocket
        {
            RocketId = @event.RocketId.Value,
            Name = @event.Name,
            ThrustCapacity = @event.ThrustCapacity,
            PayloadCapacityKg = @event.PayloadCapacityKg,
            AssignedMissionId = @event.MissionId.Value,
            Status = RocketStatus.Assigned
        };

        rocket.Status = RocketStatus.Assigned;
        rocket.AssignedMissionId = @event.MissionId.Value;

        await _rocketService.CreateOrUpdateAsync(rocket);
    }

    public async Task WhenAsync(MissionAborted @event)
    {
        // Find the rocket that was assigned to this mission
        var rocket = await _rocketService.FindByAssignedMissionAsync(@event.MissionId.Value);

        if (rocket == null)
        {
            _logger.LogWarning("No rocket found assigned to mission {MissionId}", @event.MissionId);
            throw new ReadModelException(
                $"No rocket found assigned to mission {@event.MissionId}",
                ErrorClassification.NotFound);
        }

        rocket.Status = RocketStatus.Available;
        rocket.AssignedMissionId = null;

        await _rocketService.CreateOrUpdateAsync(rocket);
    }
}
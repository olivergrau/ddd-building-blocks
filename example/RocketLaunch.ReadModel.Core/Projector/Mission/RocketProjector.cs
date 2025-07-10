using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
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
        var rocket = _rocketService.GetById(@event.RocketId.Value);

        if (rocket == null)
        {
            _logger.LogWarning("Rocket {RocketId} not found when handling RocketAssigned", @event.RocketId);
            return;
        }

        rocket.Status = RocketStatus.Assigned;
        rocket.AssignedMissionId = @event.MissionId.Value;

        await _rocketService.CreateOrUpdateAsync(rocket);
    }

    public async Task WhenAsync(MissionAborted @event)
    {
        // Find the rocket that was assigned to this mission
        var rocket = _rocketService.FindByAssignedMission(@event.MissionId.Value);

        if (rocket == null)
        {
            _logger.LogWarning("No rocket found assigned to mission {MissionId}", @event.MissionId);
            return;
        }

        rocket.Status = RocketStatus.Available;
        rocket.AssignedMissionId = null;

        await _rocketService.CreateOrUpdateAsync(rocket);
    }
}
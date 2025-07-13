using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.ReadModel.Core.Projector.Mission;

public class MissionProjector(IMissionService missionService, ICrewMemberService crewMemberService, ILogger<MissionProjector> logger)
    :
        ISubscribe<MissionCreated>,
        ISubscribe<RocketAssigned>,
        ISubscribe<LaunchPadAssigned>,
        ISubscribe<CrewAssigned>,
        ISubscribe<MissionScheduled>,
        ISubscribe<MissionAborted>,
        ISubscribe<MissionLaunched>,
        ISubscribe<MissionArrivedAtLunarOrbit>
{
    private readonly ICrewMemberService _crewMemberService = crewMemberService ?? throw new ArgumentNullException(nameof(crewMemberService));
    private readonly IMissionService _missionService = missionService ?? throw new ArgumentNullException(nameof(missionService));
    private readonly ILogger<MissionProjector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task WhenAsync(MissionCreated @event)
    {
        var mission = new Model.Mission
        {
            MissionId = @event.MissionId.Value,
            Name = @event.Name.Value,
            TargetOrbit = @event.TargetOrbit.Value,
            Payload = @event.Payload.Value,
            LaunchWindowStart = @event.LaunchWindow.Start,
            LaunchWindowEnd = @event.LaunchWindow.End,
            Status = SharedKernel.Enums.MissionStatus.Planned
        };

        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(RocketAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for RocketAssigned", @event.MissionId);
            return;
        }

        mission.AssignedRocketId = @event.RocketId.Value;
        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(LaunchPadAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for LaunchPadAssigned", @event.MissionId);
            return;
        }

        mission.AssignedPadId = @event.PadId.Value;
        mission.LaunchWindowStart = @event.LaunchWindow.Start;
        mission.LaunchWindowEnd = @event.LaunchWindow.End;

        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(CrewAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for CrewAssigned", @event.MissionId);
            return;
        }
        
        foreach (var crew in @event.Crew)
        {
            if (!mission.CrewMemberIds.Contains(crew.Value))
            {
                mission.CrewMemberIds.Add(crew.Value);
            }
            
            var member = await _crewMemberService.GetByIdAsync(crew.Value);
            if (member == null)
            {
                _logger.LogError("Crew member {CrewMemberId} not found for assignment", crew.Value);
                return;
            }
            
            member.Status = CrewMemberStatus.Assigned;
            await _crewMemberService.CreateOrUpdateAsync(member);
        }

        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(MissionScheduled @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionScheduled", @event.MissionId);
            return;
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Scheduled;
        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(MissionAborted @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionAborted", @event.MissionId);
            return;
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Aborted;
        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(MissionLaunched @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionLaunched", @event.MissionId);
            return;
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Launched;
        await _missionService.CreateOrUpdateAsync(mission);
    }

    public async Task WhenAsync(MissionArrivedAtLunarOrbit @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionArrivedAtLunarOrbit", @event.MissionId);
            return;
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Arrived;
        await _missionService.CreateOrUpdateAsync(mission);
    }
}

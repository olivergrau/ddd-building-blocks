using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Exceptions;
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

        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(RocketAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for RocketAssigned", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for rocket assignment",
                ErrorClassification.NotFound);
        }

        mission.AssignedRocketId = @event.RocketId.Value;
        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(LaunchPadAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for LaunchPadAssigned", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for launch pad assignment",
                ErrorClassification.NotFound);
        }

        mission.AssignedPadId = @event.PadId.Value;
        mission.LaunchWindowStart = @event.LaunchWindow.Start;
        mission.LaunchWindowEnd = @event.LaunchWindow.End;

        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(CrewAssigned @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for CrewAssigned", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for crew assignment",
                ErrorClassification.NotFound);
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
                throw new ReadModelException(
                    $"Crew member with ID {crew.Value} not found for assignment",
                    ErrorClassification.NotFound);
            }
            
            member.Status = CrewMemberStatus.Assigned;
            try
            {
                await _crewMemberService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }

        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(MissionScheduled @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionScheduled", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for scheduling",
                ErrorClassification.NotFound);
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Scheduled;
        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(MissionAborted @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionAborted", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for abort",
                ErrorClassification.NotFound);
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Aborted;
        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(MissionLaunched @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionLaunched", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for launch",
                ErrorClassification.NotFound);
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Launched;
        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(MissionArrivedAtLunarOrbit @event)
    {
        var mission = await _missionService.GetByIdAsync(@event.MissionId.Value);
        if (mission == null)
        {
            _logger.LogWarning("Mission {MissionId} not found for MissionArrivedAtLunarOrbit", @event.MissionId);
            throw new ReadModelException(
                $"Mission with ID {@event.MissionId} not found for arrival",
                ErrorClassification.NotFound);
        }

        mission.Status = SharedKernel.Enums.MissionStatus.Arrived;
        try
        {
            await _missionService.CreateOrUpdateAsync(mission);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }
}

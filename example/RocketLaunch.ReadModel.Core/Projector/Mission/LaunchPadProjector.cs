using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.ReadModel.Core.Projector.Mission;

public class LaunchPadProjector(ILaunchPadService padService, ILogger<LaunchPadProjector> logger)
    :
        ISubscribe<LaunchPadAssigned>,
        ISubscribe<MissionAborted>
{
    private readonly ILaunchPadService _padService = padService ?? throw new ArgumentNullException(nameof(padService));
    private readonly ILogger<LaunchPadProjector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task WhenAsync(LaunchPadAssigned @event)
    {
        var pad = await _padService.GetByIdAsync(@event.PadId.Value) ?? new LaunchPad
        {
            LaunchPadId = @event.PadId.Value,
            PadName = @event.Name,
            Location = @event.Location,
            SupportedRocketTypes = @event.SupportedRockets.ToList(),
            Status = LaunchPadStatus.Occupied,
            OccupiedWindows =
            [
                new()
                {
                    Start = @event.LaunchWindow.Start,
                    End = @event.LaunchWindow.End,
                    MissionId = @event.MissionId.Value
                }
            ]
        };

        pad.Status = LaunchPadStatus.Occupied;
            
        pad.OccupiedWindows.RemoveAll(x =>
            x.MissionId == @event.MissionId.Value);
            
        pad.OccupiedWindows.Add(new ScheduledLaunchWindow
        {
            Start = @event.LaunchWindow.Start,
            End = @event.LaunchWindow.End,
            MissionId = @event.MissionId.Value
        });

        try
        {
            await _padService.CreateOrUpdateAsync(pad);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }

    public async Task WhenAsync(MissionAborted @event)
    {
        var pad = await _padService.FindByAssignedMissionAsync(@event.MissionId.Value);

        if (pad == null)
        {
            _logger.LogWarning("No LaunchPad found assigned to mission {MissionId}", @event.MissionId);
            throw new ReadModelException(
                $"No LaunchPad found assigned to mission {@event.MissionId}",
                ErrorClassification.NotFound);
        }

        pad.OccupiedWindows.RemoveAll(w => w.MissionId == @event.MissionId.Value);

        pad.Status = pad.OccupiedWindows.Count == 0
            ? LaunchPadStatus.Available
            : LaunchPadStatus.Occupied;

        try
        {
            await _padService.CreateOrUpdateAsync(pad);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }
        
    public async Task WhenAsync(MissionLaunched @event)
    {
        var pad = await _padService.FindByAssignedMissionAsync(@event.MissionId.Value);

        if (pad == null)
        {
            _logger.LogWarning("LaunchPad not found for launched mission {MissionId}", @event.MissionId);
            throw new ReadModelException(
                $"No LaunchPad found assigned to mission {@event.MissionId}",
                ErrorClassification.NotFound);
        }

        pad.OccupiedWindows.RemoveAll(w => w.MissionId == @event.MissionId.Value);
        pad.Status = pad.OccupiedWindows.Count == 0 ? LaunchPadStatus.Available : LaunchPadStatus.Occupied;

        try
        {
            await _padService.CreateOrUpdateAsync(pad);
        }
        catch (Exception ex)
        {
            throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
        }
    }
}
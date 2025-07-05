using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events;

namespace RocketLaunch.ReadModel.Core.Builder
{
    public class LaunchPadBuilder(ILaunchPadService padService, ILogger<LaunchPadBuilder> logger)
        :
            ISubscribe<LaunchPadAssigned>,
            ISubscribe<MissionAborted>
    {
        private readonly ILaunchPadService _padService = padService ?? throw new ArgumentNullException(nameof(padService));
        private readonly ILogger<LaunchPadBuilder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task WhenAsync(LaunchPadAssigned @event)
        {
            var pad = _padService.GetById(@event.PadId.Value);

            if (pad == null)
            {
                _logger.LogWarning("LaunchPad {PadId} not found when handling LaunchPadAssigned", @event.PadId);
                return;
            }

            pad.Status = LaunchPadStatus.Occupied;

            pad.OccupiedWindows.Add(new ScheduledLaunchWindow
            {
                Start = @event.LaunchWindow.Start,
                End = @event.LaunchWindow.End,
                MissionId = @event.MissionId.Value
            });

            await _padService.CreateOrUpdateAsync(pad);
        }

        public async Task WhenAsync(MissionAborted @event)
        {
            var pad = _padService.FindByAssignedMission(@event.MissionId.Value);

            if (pad == null)
            {
                _logger.LogWarning("No LaunchPad found assigned to mission {MissionId}", @event.MissionId);
                return;
            }

            pad.OccupiedWindows.RemoveAll(w => w.MissionId == @event.MissionId.Value);

            pad.Status = pad.OccupiedWindows.Count == 0
                ? LaunchPadStatus.Available
                : LaunchPadStatus.Occupied;

            await _padService.CreateOrUpdateAsync(pad);
        }
        
        public async Task WhenAsync(MissionLaunched @event)
        {
            var pad = _padService.FindByAssignedMission(@event.MissionId.Value);

            if (pad == null)
            {
                _logger.LogWarning("LaunchPad not found for launched mission {MissionId}", @event.MissionId);
                return;
            }

            pad.OccupiedWindows.RemoveAll(w => w.MissionId == @event.MissionId.Value);
            pad.Status = pad.OccupiedWindows.Count == 0 ? LaunchPadStatus.Available : LaunchPadStatus.Occupied;

            await _padService.CreateOrUpdateAsync(pad);
        }
    }
}

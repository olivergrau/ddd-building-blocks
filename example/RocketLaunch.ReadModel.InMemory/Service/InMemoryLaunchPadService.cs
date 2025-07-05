using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryLaunchPadService : ILaunchPadService
    {
        private readonly ConcurrentDictionary<Guid, LaunchPad> _pads = new();

        public LaunchPad? GetById(Guid padId)
        {
            _pads.TryGetValue(padId, out var pad);
            return pad;
        }

        public bool IsAvailable(Guid padId, DateTime windowStart, DateTime windowEnd)
        {
            var pad = GetById(padId);
            if (pad == null || pad.Status == LaunchPadStatus.UnderMaintenance)
                return false;

            return !pad.OccupiedWindows.Any(w => w.Start < windowEnd && windowStart < w.End);
        }

        public LaunchPad? FindByAssignedMission(Guid missionId)
        {
            return _pads.Values.FirstOrDefault(p =>
                p.OccupiedWindows.Any(w => w.MissionId == missionId));
        }

        public IEnumerable<LaunchPad> FindAvailable(string rocketType, DateTime windowStart, DateTime windowEnd)
        {
            return _pads.Values.Where(p =>
                p.Status != LaunchPadStatus.UnderMaintenance &&
                p.SupportedRocketTypes.Contains(rocketType) &&
                !p.OccupiedWindows.Any(w => w.Start < windowEnd && windowStart < w.End));
        }

        public Task CreateOrUpdateAsync(LaunchPad pad)
        {
            _pads[pad.LaunchPadId] = pad;
            return Task.CompletedTask;
        }
    }
}
using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryLaunchPadService : ILaunchPadService
    {
        private readonly ConcurrentDictionary<Guid, LaunchPad> _pads = new();

        public Task<LaunchPad?> GetByIdAsync(Guid padId)
        {
            _pads.TryGetValue(padId, out var pad);
            return Task.FromResult(pad);
        }

        public Task<IEnumerable<LaunchPad>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<LaunchPad>>(_pads.Values);
        }

        public async Task<bool> IsAvailableAsync(Guid padId, DateTime windowStart, DateTime windowEnd)
        {
            var pad = await GetByIdAsync(padId);
            
            if (pad == null)
                return true;
            
            if (pad.Status == LaunchPadStatus.UnderMaintenance)
                return false;

            var overlaps = pad.OccupiedWindows.Any(w =>
                w.Start < windowEnd && windowStart < w.End);

            return !overlaps && pad.Status == LaunchPadStatus.Available;
        }

        public Task<LaunchPad?> FindByAssignedMissionAsync(Guid missionId)
        {
            var pad = _pads.Values.FirstOrDefault(p =>
                p.OccupiedWindows.Any(w => w.MissionId == missionId));
            return Task.FromResult(pad);
        }

        public Task<IEnumerable<LaunchPad>> FindAvailableAsync(string rocketType, DateTime windowStart, DateTime windowEnd)
        {
            var result = _pads.Values.Where(p =>
                p.Status != LaunchPadStatus.UnderMaintenance &&
                p.SupportedRocketTypes.Contains(rocketType) &&
                !p.OccupiedWindows.Any(w => w.Start < windowEnd && windowStart < w.End));
            return Task.FromResult<IEnumerable<LaunchPad>>(result);
        }

        public Task CreateOrUpdateAsync(LaunchPad pad)
        {
            _pads[pad.LaunchPadId] = pad;
            return Task.CompletedTask;
        }
    }
}
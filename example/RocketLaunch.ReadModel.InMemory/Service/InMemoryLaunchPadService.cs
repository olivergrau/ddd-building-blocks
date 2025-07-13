using System.Collections.Concurrent;
using DDD.BuildingBlocks.Core.ErrorHandling;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryLaunchPadService : ILaunchPadService
    {
        private readonly ConcurrentDictionary<Guid, LaunchPad> _pads = new();

        public Task<LaunchPad?> GetByIdAsync(Guid padId)
        {
            if (padId == Guid.Empty)
                throw new ReadModelServiceException("Invalid launch pad id", ErrorClassification.InputDataError);

            _pads.TryGetValue(padId, out var pad);
            return Task.FromResult(pad);
        }

        public Task<IEnumerable<LaunchPad>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<LaunchPad>>(_pads.Values);
        }

        public async Task<bool> IsAvailableAsync(Guid padId, DateTime windowStart, DateTime windowEnd)
        {
            if (padId == Guid.Empty)
                throw new ReadModelServiceException("Invalid launch pad id", ErrorClassification.InputDataError);

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
            if (missionId == Guid.Empty)
                throw new ReadModelServiceException("Invalid mission id", ErrorClassification.InputDataError);

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
using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryRocketService : IRocketService
    {
        private readonly ConcurrentDictionary<Guid, Rocket> _rockets = new();

        public Task<Rocket?> GetByIdAsync(Guid rocketId)
        {
            _rockets.TryGetValue(rocketId, out var rocket);
            return Task.FromResult(rocket);
        }

        public Task<IEnumerable<Rocket>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Rocket>>(_rockets.Values);
        }

        public Task<Rocket?> FindByAssignedMissionAsync(Guid missionId)
        {
            var rocket = _rockets.Values.FirstOrDefault(r => r.AssignedMissionId == missionId);
            return Task.FromResult(rocket);
        }

        public async Task<bool> IsAvailableAsync(Guid rocketId)
        {
            var rocket = await GetByIdAsync(rocketId);
            if (rocket == null)
                return false;
            return rocket.Status == RocketStatus.Available;
        }

        public Task<IEnumerable<Rocket>> FindAvailableAsync(int minPayloadKg, int minCrewCapacity)
        {
            var result = _rockets.Values.Where(r =>
                r.Status == RocketStatus.Available &&
                r.PayloadCapacityKg >= minPayloadKg &&
                r.CrewCapacity >= minCrewCapacity);
            return Task.FromResult<IEnumerable<Rocket>>(result);
        }

        public Task CreateOrUpdateAsync(Rocket rocket)
        {
            _rockets[rocket.RocketId] = rocket;
            return Task.CompletedTask;
        }
    }
}
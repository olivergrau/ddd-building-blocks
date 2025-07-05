using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryRocketService : IRocketService
    {
        private readonly ConcurrentDictionary<Guid, Rocket> _rockets = new();

        public Rocket? GetById(Guid rocketId)
        {
            _rockets.TryGetValue(rocketId, out var rocket);
            return rocket;
        }

        public Rocket? FindByAssignedMission(Guid missionId)
        {
            return _rockets.Values.FirstOrDefault(r => r.AssignedMissionId == missionId);
        }
        
        public bool IsAvailable(Guid rocketId)
        {
            var rocket = GetById(rocketId);
            if (rocket == null)
                return false;
            return rocket.Status == RocketStatus.Available;
        }

        public IEnumerable<Rocket> FindAvailable(int minPayloadKg, int minCrewCapacity)
        {
            return _rockets.Values.Where(r =>
                r.Status == RocketStatus.Available &&
                r.PayloadCapacityKg >= minPayloadKg &&
                r.CrewCapacity >= minCrewCapacity);
        }

        public Task CreateOrUpdateAsync(Rocket rocket)
        {
            _rockets[rocket.RocketId] = rocket;
            return Task.CompletedTask;
        }
    }
}
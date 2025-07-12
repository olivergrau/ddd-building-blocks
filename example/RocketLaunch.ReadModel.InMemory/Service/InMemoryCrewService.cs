using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryCrewService(IMissionService missionService) : ICrewMemberService
    {
        private readonly ConcurrentDictionary<Guid, CrewMember> _crew = new();
        private readonly IMissionService _missionService = missionService;

        public CrewMember? GetById(Guid id)
        {
            _crew.TryGetValue(id, out var member);
            return member;
        }

        public bool IsAvailable(Guid crewMemberId, string requiredRole)
        {
            if (!_crew.TryGetValue(crewMemberId, out var member))
                return false;

            return member.Status == CrewMemberStatus.Available && member.Role == requiredRole;
        }

        public IEnumerable<CrewMember> FindByAssignedMission(Guid missionId)
        {
            var mission = _missionService.GetById(missionId);
            if (mission == null)
                return Enumerable.Empty<CrewMember>();

            return mission.CrewMemberIds
                .Select(id => GetById(id))
                .Where(m => m != null)
                .Select(m => m!);
        }

        public IEnumerable<CrewMember> FindAvailable(string role, string? certification = null)
        {
            return _crew.Values.Where(c =>
                c.Status == CrewMemberStatus.Available &&
                c.Role == role &&
                (certification == null || c.CertificationLevels.Contains(certification)));
        }

        public Task CreateOrUpdateAsync(CrewMember member)
        {
            _crew[member.CrewMemberId] = member;
            return Task.CompletedTask;
        }
    }
}
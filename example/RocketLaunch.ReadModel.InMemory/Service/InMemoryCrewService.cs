using System.Collections.Concurrent;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryCrewService : ICrewMemberService
    {
        private readonly ConcurrentDictionary<Guid, CrewMember> _crew = new();

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
            return _crew.Values.Where(c => c.AssignedMissionId == missionId);
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
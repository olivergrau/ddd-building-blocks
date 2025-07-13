using System.Collections.Concurrent;
using DDD.BuildingBlocks.Core.ErrorHandling;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;

namespace RocketLaunch.ReadModel.InMemory.Service
{
    public class InMemoryCrewService(IMissionService missionService) : ICrewMemberService
    {
        private readonly ConcurrentDictionary<Guid, CrewMember> _crew = new();
        private readonly IMissionService _missionService = missionService;

        public Task<CrewMember?> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ReadModelServiceException("Invalid crew member id", ErrorClassification.InputDataError);

            _crew.TryGetValue(id, out var member);
            return Task.FromResult(member);
        }

        public Task<IEnumerable<CrewMember>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<CrewMember>>(_crew.Values);
        }

        public Task<bool> IsAvailableAsync(Guid crewMemberId, string requiredRole)
        {
            if (crewMemberId == Guid.Empty)
                throw new ReadModelServiceException("Invalid crew member id", ErrorClassification.InputDataError);

            if (!_crew.TryGetValue(crewMemberId, out var member))
                return Task.FromResult(false);

            return Task.FromResult(member.Status == CrewMemberStatus.Available && member.Role == requiredRole);
        }

        public async Task<IEnumerable<CrewMember>> FindByAssignedMissionAsync(Guid missionId)
        {
            if (missionId == Guid.Empty)
                throw new ReadModelServiceException("Invalid mission id", ErrorClassification.InputDataError);

            var mission = await _missionService.GetByIdAsync(missionId);
            if (mission == null)
                return [];

            var members = await Task.WhenAll(mission.CrewMemberIds.Select(id => GetByIdAsync(id)));
            return members.Where(m => m != null)!.Select(m => m!);
        }

        public Task<IEnumerable<CrewMember>> FindAvailableAsync(string role, string? certification = null)
        {
            var result = _crew.Values.Where(c =>
                c.Status == CrewMemberStatus.Available &&
                c.Role == role &&
                (certification == null || c.CertificationLevels.Contains(certification)));
            return Task.FromResult<IEnumerable<CrewMember>>(result);
        }

        public Task CreateOrUpdateAsync(CrewMember member)
        {
            _crew[member.CrewMemberId] = member;
            return Task.CompletedTask;
        }
    }
}
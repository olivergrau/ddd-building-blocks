using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.ReadModel.Core.Builder
{
    public class CrewMemberProjector(ICrewMemberService crewService, ILogger<CrewMemberProjector> logger)
        :
            ISubscribe<CrewAssigned>,
            ISubscribe<MissionAborted>,
            ISubscribe<MissionLaunched>
    {
        private readonly ICrewMemberService _crewService = crewService ?? throw new ArgumentNullException(nameof(crewService));
        private readonly ILogger<CrewMemberProjector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task WhenAsync(CrewAssigned @event)
        {
            foreach (var crewId in @event.Crew)
            {
                var member = _crewService.GetById(crewId.Value) ?? new CrewMember
                {
                    CrewMemberId = crewId.Value,
                    Status = CrewMemberStatus.Unknown,
                    CertificationLevels = []
                };

                member.Status = CrewMemberStatus.Assigned;
                member.AssignedMissionId = @event.MissionId.Value;

                await _crewService.CreateOrUpdateAsync(member);
            }
        }

        public async Task WhenAsync(MissionAborted @event)
        {
            var crewMembers = _crewService.FindByAssignedMission(@event.MissionId.Value);
            foreach (var member in crewMembers)
            {
                member.Status = CrewMemberStatus.Available;
                member.AssignedMissionId = null;
                await _crewService.CreateOrUpdateAsync(member);
            }
        }

        public async Task WhenAsync(MissionLaunched @event)
        {
            var crewMembers = _crewService.FindByAssignedMission(@event.MissionId.Value);
            foreach (var member in crewMembers)
            {
                member.Status = CrewMemberStatus.Available;
                member.AssignedMissionId = null;
                await _crewService.CreateOrUpdateAsync(member);
            }
        }
    }
}

using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Model;
using RocketLaunch.ReadModel.Core.Service;
using RocketLaunch.SharedKernel.Events.CrewMember;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.ReadModel.Core.Projector.CrewMember
{
    public class CrewMemberProjector(ICrewMemberService crewService, ILogger<CrewMemberProjector> logger)
        :
            ISubscribe<CrewMemberAssigned>,
            ISubscribe<CrewMemberCertificationSet>,
            ISubscribe<CrewMemberRegistered>,
            ISubscribe<CrewMemberReleased>,
            ISubscribe<CrewMemberStatusSet>
    {
        private readonly ICrewMemberService _crewService = crewService ?? throw new ArgumentNullException(nameof(crewService));
        private readonly ILogger<CrewMemberProjector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


        public async Task WhenAsync(CrewMemberAssigned @event)
        {
            var member = _crewService.GetById(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for assignment", @event.CrewMemberId);
                return;
            }

            member.Status = CrewMemberStatus.Assigned;
            await _crewService.CreateOrUpdateAsync(member);
        }

        public async Task WhenAsync(CrewMemberCertificationSet @event)
        {
            var member = _crewService.GetById(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for certification update", @event.CrewMemberId);
                return;
            }

            member.CertificationLevels = new List<string>(@event.Certifications);
            await _crewService.CreateOrUpdateAsync(member);
        }

        public async Task WhenAsync(CrewMemberRegistered @event)
        {
            var member = new Model.CrewMember
            {
                CrewMemberId = @event.CrewMemberId.Value,
                Name = @event.Name,
                Role = @event.Role.ToString(),
                CertificationLevels = new List<string>(@event.Certifications),
                Status = CrewMemberStatus.Available
            };

            await _crewService.CreateOrUpdateAsync(member);
        }

        public async Task WhenAsync(CrewMemberReleased @event)
        {
            var member = _crewService.GetById(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for release", @event.CrewMemberId);
                return;
            }

            member.Status = CrewMemberStatus.Available;
            await _crewService.CreateOrUpdateAsync(member);
        }

        public async Task WhenAsync(CrewMemberStatusSet @event)
        {
            var member = _crewService.GetById(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for status change", @event.CrewMemberId);
                return;
            }

            member.Status = @event.Status switch
            {
                RocketLaunch.SharedKernel.Enums.CrewMemberStatus.Available => CrewMemberStatus.Available,
                RocketLaunch.SharedKernel.Enums.CrewMemberStatus.Assigned => CrewMemberStatus.Assigned,
                RocketLaunch.SharedKernel.Enums.CrewMemberStatus.Unavailable => CrewMemberStatus.Unavailable,
                _ => CrewMemberStatus.Unknown
            };
            await _crewService.CreateOrUpdateAsync(member);
        }
    }
}

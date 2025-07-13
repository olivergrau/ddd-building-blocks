using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Event;
using Microsoft.Extensions.Logging;
using RocketLaunch.ReadModel.Core.Exceptions;
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
            var member = await _crewService.GetByIdAsync(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for assignment", @event.CrewMemberId);
                throw new ReadModelException(
                    $"Crew member with ID {@event.CrewMemberId} not found for assignment",
                    ErrorClassification.NotFound);
            }

            member.Status = CrewMemberStatus.Assigned;
            try
            {
                await _crewService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }

        public async Task WhenAsync(CrewMemberCertificationSet @event)
        {
            var member = await _crewService.GetByIdAsync(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for certification update", @event.CrewMemberId);
                throw new ReadModelException(
                    $"Crew member with ID {@event.CrewMemberId} not found for assignment",
                    ErrorClassification.NotFound);
            }

            member.CertificationLevels = new List<string>(@event.Certifications);
            try
            {
                await _crewService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }

        public async Task WhenAsync(CrewMemberRegistered @event)
        {
            var member = new Model.CrewMember
            {
                CrewMemberId = @event.CrewMemberId.Value,
                Name = @event.Name,
                Role = @event.Role.ToString(),
                CertificationLevels = [..@event.Certifications],
                Status = CrewMemberStatus.Available
            };

            try
            {
                await _crewService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }

        public async Task WhenAsync(CrewMemberReleased @event)
        {
            var member = await _crewService.GetByIdAsync(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for release", @event.CrewMemberId);
                throw new ReadModelException(
                    $"Crew member with ID {@event.CrewMemberId} not found for assignment",
                    ErrorClassification.NotFound);
            }

            member.Status = CrewMemberStatus.Available;
            try
            {
                await _crewService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }

        public async Task WhenAsync(CrewMemberStatusSet @event)
        {
            var member = await _crewService.GetByIdAsync(@event.CrewMemberId.Value);
            if (member == null)
            {
                _logger.LogWarning("Crew member {CrewMemberId} not found for status change", @event.CrewMemberId);
                throw new ReadModelException(
                    $"Crew member with ID {@event.CrewMemberId} not found for assignment",
                    ErrorClassification.NotFound);
            }

            member.Status = @event.Status switch
            {
                SharedKernel.Enums.CrewMemberStatus.Available => CrewMemberStatus.Available,
                SharedKernel.Enums.CrewMemberStatus.Assigned => CrewMemberStatus.Assigned,
                SharedKernel.Enums.CrewMemberStatus.Unavailable => CrewMemberStatus.Unavailable,
                _ => CrewMemberStatus.Unknown
            };
            try
            {
                await _crewService.CreateOrUpdateAsync(member);
            }
            catch (Exception ex)
            {
                throw new ReadModelServiceException(ex.Message, ErrorClassification.Infrastructure);
            }
        }
    }
}

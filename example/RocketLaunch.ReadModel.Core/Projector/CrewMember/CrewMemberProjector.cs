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


        public Task WhenAsync(CrewMemberAssigned @event)
        {
            throw new NotImplementedException();
        }

        public Task WhenAsync(CrewMemberCertificationSet @event)
        {
            throw new NotImplementedException();
        }

        public Task WhenAsync(CrewMemberRegistered @event)
        {
            throw new NotImplementedException();
        }

        public Task WhenAsync(CrewMemberReleased @event)
        {
            throw new NotImplementedException();
        }

        public Task WhenAsync(CrewMemberStatusSet @event)
        {
            throw new NotImplementedException();
        }
    }
}

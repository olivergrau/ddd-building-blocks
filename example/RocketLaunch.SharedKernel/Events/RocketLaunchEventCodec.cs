using DDD.BuildingBlocks.Core.Event.Serialization;
using RocketLaunch.SharedKernel.Events.CrewMember;
using RocketLaunch.SharedKernel.Events.Mission;

namespace RocketLaunch.SharedKernel.Events;

public static class RocketLaunchEventCodec
{
    public static IEventCodec Create()
    {
        var registry = new EventTypeRegistry()
            .Register<CrewMemberAssigned>("crew-member.assigned")
            .Register<CrewMemberCertificationSet>("crew-member.certification-set")
            .Register<CrewMemberRegistered>("crew-member.registered")
            .Register<CrewMemberReleased>("crew-member.released")
            .Register<CrewMemberStatusSet>("crew-member.status-set")
            .Register<CrewAssigned>("mission.crew-assigned")
            .Register<LaunchPadAssigned>("mission.launch-pad-assigned")
            .Register<MissionAborted>("mission.aborted")
            .Register<MissionArrivedAtLunarOrbit>("mission.arrived-at-lunar-orbit")
            .Register<MissionCreated>("mission.created")
            .Register<MissionLaunched>("mission.launched")
            .Register<MissionScheduled>("mission.scheduled")
            .Register<RocketAssigned>("mission.rocket-assigned");

        return new SystemTextJsonEventCodec(registry);
    }
}

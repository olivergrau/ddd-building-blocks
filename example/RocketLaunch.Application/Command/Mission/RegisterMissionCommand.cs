// RocketLaunchScheduling.Domain/Commands/MissionCommands.cs

using DDD.BuildingBlocks.Core.Commanding;
using RocketLaunch.Application.Dto;

namespace RocketLaunch.Application.Command.Mission
{
    /// <summary>
    /// Command to create a new mission in the "Planned" state.
    /// 
    /// Preconditions:
    /// - MissionId must be unique.
    /// 
    /// Side Effects:
    /// - Emits MissionCreated domain event.
    /// </summary>
    public class RegisterMissionCommand : DDD.BuildingBlocks.Core.Commanding.Command
    {
        public Guid MissionId { get; }
        public string? MissionName { get; }
        public string? TargetOrbit { get; }
        public string? PayloadDescription { get; }
        public LaunchWindowDto LaunchWindow { get; }

        public RegisterMissionCommand(
            Guid missionId,
            string? missionName,
            string? targetOrbit,
            string? payloadDescription,
            LaunchWindowDto launchWindow
        ) : base(missionId.ToString(), -1)
        {
            MissionId          = missionId;
            MissionName        = missionName ?? throw new ArgumentNullException(nameof(missionName));
            TargetOrbit        = targetOrbit ?? throw new ArgumentNullException(nameof(targetOrbit));
            PayloadDescription = payloadDescription ?? throw new ArgumentNullException(nameof(payloadDescription));
            LaunchWindow       = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));

            Mode = AggregateSourcingMode.Create;
        }
    }
}

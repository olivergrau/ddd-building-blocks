// Domain/Entities/LaunchPad.cs

using DDD.BuildingBlocks.Core.Domain;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model.Entities
{
    public class LaunchPad : Entity<LaunchPadId>
    {
        public LaunchPad(LaunchPadId id, string name, string location, string[] supportedRockets)
            : base(id)
        {
            Name = name;
            Location = location;
            SupportedRocketTypes = supportedRockets;
            Status = LaunchPadStatus.Available;
        }

        private LaunchPad() : base(default!) { }

        public string           Name                 { get; private set; } = null!;
        public string           Location             { get; private set; } = null!;
        public IReadOnlyList<string> SupportedRocketTypes { get; private set; } = null!;
        public LaunchPadStatus  Status               { get; private set; }
    }
}
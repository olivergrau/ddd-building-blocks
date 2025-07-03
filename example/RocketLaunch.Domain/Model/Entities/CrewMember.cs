// Domain/Entities/CrewMember.cs

using DDD.BuildingBlocks.Core.Domain;
using RocketLaunch.SharedKernel.Enums;
using RocketLaunch.SharedKernel.ValueObjects;

namespace RocketLaunch.Domain.Model.Entities
{
    public class CrewMember : Entity<CrewMemberId>
    {
        public CrewMember(CrewMemberId id, string name, CrewRole role, string[] certifications)
            : base(id)
        {
            Name = name;
            Role = role;
            Certifications = certifications;
            Status = CrewMemberStatus.Available;
        }

        private CrewMember() : base(default!) { }

        public string             Name           { get; private set; } = null!;
        public CrewRole           Role           { get; private set; }
        public IReadOnlyList<string> Certifications { get; private set; } = null!;
        public CrewMemberStatus   Status         { get; private set; }

        public void Assign()
        {
            if (Status != CrewMemberStatus.Available)
                throw new Exception("Crew member is not available");
            Status = CrewMemberStatus.Assigned;
        }

        public void Release()
        {
            Status = CrewMemberStatus.Available;
        }
    }
}
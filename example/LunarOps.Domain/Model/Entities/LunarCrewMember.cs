// Domain/Entities/LunarCrewMember.cs

using DDD.BuildingBlocks.Core.Domain;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model.Entities
{
    public class LunarCrewMember : Entity<LunarCrewMemberId>
    {
        public string                 Name            { get; private set; }
        public string                 Role            { get; private set; }
        public CrewAssignmentStatus   AssignmentStatus{ get; private set; }

        public LunarCrewMember(
            LunarCrewMemberId id,
            string name,
            string role
        ) : base(id)
        {
            Name             = name;
            Role             = role;
            AssignmentStatus = CrewAssignmentStatus.OffDuty;
        }

        private LunarCrewMember() : base(default!) { }

        public void Activate()   => AssignmentStatus = CrewAssignmentStatus.Active;
        public void Return()     => AssignmentStatus = CrewAssignmentStatus.Returned;
        public void OffDuty()    => AssignmentStatus = CrewAssignmentStatus.OffDuty;
    }
}
// Domain/Entities/DockingPort.cs

using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model.Entities
{
    public class DockingPort : Entity<DockingPortId>
    {
        public DockingPortStatus Status          { get; private set; }
        public VehicleType?           AssignedVehicle { get; private set; }

        public DockingPort(DockingPortId id) : base(id)
        {
            Status = DockingPortStatus.Available;
        }

        private DockingPort() : base(default!) { }

        public void Occupy(VehicleType vehicle)
        {
            if (Status != DockingPortStatus.Available)
                throw new EntityValidationException(Id, nameof(Status), Status, "Port not available");
            Status = DockingPortStatus.Occupied;
            AssignedVehicle = vehicle;
        }

        public void Release()
        {
            Status = DockingPortStatus.Available;
            AssignedVehicle = null;
        }
    }
}
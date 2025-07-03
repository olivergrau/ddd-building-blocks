// Domain/Entities/MoonStation.cs

using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using LunarOps.SharedKernel.Enums;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Model.Entities
{
    public class MoonStation : Entity<StationId>
    {
        public string                Name                   { get; private set; }
        public string                Location               { get; private set; }
        public IReadOnlyList<string> SupportedVehicleTypes  { get; private set; }
        public StationStatus         Status                 { get; private set; }
        public int                   TotalPorts             { get; private set; }
        public int                   AvailablePorts         { get; private set; }
        public int                   CrewCapacity           { get; private set; }

        public MoonStation(
            StationId id,
            string name,
            string location,
            IEnumerable<string> supportedVehicles,
            int totalPorts,
            int crewCapacity
        ) : base(id)
        {
            Name                  = name;
            Location              = location;
            SupportedVehicleTypes = new List<string>(supportedVehicles).AsReadOnly();
            TotalPorts            = totalPorts;
            AvailablePorts        = totalPorts;
            CrewCapacity          = crewCapacity;
            Status                = StationStatus.Active;
        }

        private MoonStation() : base(default!) { }

        public void ReservePort()
        {
            if (AvailablePorts <= 0)
                throw new EntityValidationException(Id, nameof(AvailablePorts), AvailablePorts,"No docking ports available");
            AvailablePorts--;
        }

        public void ReleasePort()
        {
            if (AvailablePorts >= TotalPorts)
                throw new EntityValidationException(Id, nameof(AvailablePorts), AvailablePorts, "All ports are already free");
            AvailablePorts++;
        }

        public void SetStatus(StationStatus status) => Status = status;
    }
}
// Domain/Services/IStationAvailabilityService.cs

using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Service
{
    public interface IStationAvailabilityService
    {
        Task<bool> HasFreePortAsync(StationId stationId);
        Task<bool> HasCrewCapacityAsync(StationId stationId, int crewCount);
        Task<bool> HasStorageCapacityAsync(StationId stationId, double payloadMass);
        Task<bool> HasSupportedVehicleTypeAsync(StationId stationId, VehicleType vehicleType);
    }
}
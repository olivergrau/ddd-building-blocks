// LunarOps.Domain.Tests/Mocks/StubStationAvailabilityService.cs
using LunarOps.Domain.Service;
using LunarOps.SharedKernel.ValueObjects;

namespace LunarOps.Domain.Tests.Mocks;

public class StubStationAvailabilityService : IStationAvailabilityService
{
    public bool HasFreePort      { get; set; } = true;
    public bool HasCrewCapacity  { get; set; } = true;
    public bool HasStorage       { get; set; } = true;
    public bool HasSupportedVehicleType { get; set; } = true;

    public Task<bool> HasFreePortAsync(StationId stationId)
        => Task.FromResult(HasFreePort);

    public Task<bool> HasCrewCapacityAsync(StationId stationId, int crewCount)
        => Task.FromResult(HasCrewCapacity);

    public Task<bool> HasStorageCapacityAsync(StationId stationId, double payloadMass)
        => Task.FromResult(HasStorage);

    public Task<bool> HasSupportedVehicleTypeAsync(StationId stationId, VehicleType vehicleType)
    {
        return Task.FromResult(HasSupportedVehicleType);
    }
}
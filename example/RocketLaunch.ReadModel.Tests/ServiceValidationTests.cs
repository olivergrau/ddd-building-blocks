using System;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.ErrorHandling;
using RocketLaunch.ReadModel.Core.Exceptions;
using RocketLaunch.ReadModel.InMemory.Service;
using Xunit;

namespace RocketLaunch.ReadModel.Tests;

public class ServiceValidationTests
{
    [Fact]
    public async Task RocketService_rejects_empty_id()
    {
        var service = new InMemoryRocketService();
        var ex = await Assert.ThrowsAsync<ReadModelServiceException>(() => service.GetByIdAsync(Guid.Empty));
        Assert.Equal(ErrorClassification.InputDataError, ex.ErrorInfo.Classification);
    }

    [Fact]
    public async Task LaunchPadService_rejects_empty_id()
    {
        var service = new InMemoryLaunchPadService();
        var ex = await Assert.ThrowsAsync<ReadModelServiceException>(() => service.GetByIdAsync(Guid.Empty));
        Assert.Equal(ErrorClassification.InputDataError, ex.ErrorInfo.Classification);
    }

    [Fact]
    public async Task MissionService_rejects_empty_id()
    {
        var service = new InMemoryMissionService();
        var ex = await Assert.ThrowsAsync<ReadModelServiceException>(() => service.GetByIdAsync(Guid.Empty));
        Assert.Equal(ErrorClassification.InputDataError, ex.ErrorInfo.Classification);
    }

    [Fact]
    public async Task CrewService_rejects_empty_id()
    {
        var service = new InMemoryCrewService(new InMemoryMissionService());
        var ex = await Assert.ThrowsAsync<ReadModelServiceException>(() => service.GetByIdAsync(Guid.Empty));
        Assert.Equal(ErrorClassification.InputDataError, ex.ErrorInfo.Classification);
    }
}

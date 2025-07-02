namespace DDD.BuildingBlocks.AzurePackage.DomainEventHandler;

public class ServiceBusConfiguration
{
    public string? QueueName { get; set; } = default!;
    public string? ConnectionString { get; set; } = default!;
}

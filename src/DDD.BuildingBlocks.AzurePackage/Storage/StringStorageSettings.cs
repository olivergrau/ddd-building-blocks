namespace DDD.BuildingBlocks.AzurePackage.Storage;

public class StringStorageSettings
{
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; } = "messages";

}

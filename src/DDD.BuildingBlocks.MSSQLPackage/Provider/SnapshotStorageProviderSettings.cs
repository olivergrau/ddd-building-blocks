namespace DDD.BuildingBlocks.MSSQLPackage.Provider;

public class SnapshotStorageProviderSettings
{
    public string ConnectionString { get; set; } = default!;
    public int SnapshotFrequency { get; set; } = 100;
}

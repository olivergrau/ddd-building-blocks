namespace DDD.BuildingBlocks.MSSQLPackage.Service;

public enum OptionsSetNames
{
    OptionsSet1, OptionsSet2, OptionsSet3, OptionsSet4, OptionsSet5
}

public class EventProcessingServiceBackgroundWorkerSettings
{
    public const string OptionsSet1 = "OptionsSet1";
    public const string OptionsSet2 = "OptionsSet2";
    public const string OptionsSet3 = "OptionsSet3";
    public const string OptionsSet4 = "OptionsSet4";
    public const string OptionsSet5 = "OptionsSet5";

    public string ConnectionString { get; set; } = default!;

    public string[]? OnlyAllowSendingForCategories { get; set; } = default!;
}

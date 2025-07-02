namespace ManagementAPI.BFF.BlobStorageService;

public static class ConfigurationKeys
{
    public const string BlobStorageSettingsConnectionString = "APIM:ContentBlobStorage:ConnectionString";
    public const string BlobStorageSettingsTempContentContainerName = "APIM:ContentBlobStorage:ContainerNameForTemporaryStorage";
    public const string BlobStorageSettingsCommittedContentContainerName = "APIM:ContentBlobStorage:ContainerNameForCommittedStorage";
}

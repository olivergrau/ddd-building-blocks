using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.AzurePackage.Storage;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Core.Persistence.Storage;
using Microsoft.Extensions.Options;

public class AzureStringStorageService : IStringStorageService
{
private readonly IOptionsMonitor<StringStorageSettings> _settingsMonitor;

    public AzureStringStorageService(IOptionsMonitor<StringStorageSettings> settingsMonitor)
    {
        _settingsMonitor = settingsMonitor;

        if (string.IsNullOrWhiteSpace(_settingsMonitor.CurrentValue.ConnectionString))
        {
            throw new Exception("No connection string for blob storage connection");
        }
    }

    public async Task SaveAsync(string content, string key)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new Exception($"No content to commit");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        await SaveContentInternalAsync(content, _settingsMonitor.CurrentValue.ContainerName ?? throw new Exception("No container name provided"), key);
    }

    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        var content = await GetFromStorageInternalAsync(key, _settingsMonitor.CurrentValue.ContainerName ?? throw new Exception("No container name provided"));

        return content;
    }

    public async Task DeleteAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        var containerClient = new BlobContainerClient(
            _settingsMonitor.CurrentValue.ConnectionString, _settingsMonitor.CurrentValue.ContainerName ?? throw new Exception("No container name provided"));

        await containerClient.DeleteBlobAsync(
            key, DeleteSnapshotsOption.IncludeSnapshots);
    }

    private async Task<string?> GetFromStorageInternalAsync(string key, string containerName)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new Exception("No container name provided");
        }

        var containerClient = new BlobContainerClient(
            _settingsMonitor.CurrentValue.ConnectionString,  containerName);

        var blobClient = containerClient.GetBlobClient(key);

        if (!await blobClient.ExistsAsync())
        {
            return null;
        }

        var content = await blobClient.DownloadAsync();

        using var reader = new StreamReader(content.Value.Content);
        var blobContent = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(blobContent))
        {
            throw new Exception("Blob could not be read");
        }

        return blobContent;
    }

    private async Task SaveContentInternalAsync(string content, string containerName, string key)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(content));
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(containerName));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
        }

        var containerClient = GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(
            key);

        var ms = new MemoryStream();
        await using var writer = new StreamWriter(ms);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        ms.Position = 0;

        await blobClient.UploadAsync(ms, true); // <- very important to set overwrite to true
        await blobClient.SetHttpHeadersAsync(
            new BlobHttpHeaders { ContentType = "application/file" });
    }

    private BlobContainerClient GetBlobContainerClient(string containerName)
    {
        var blobServiceClient = new BlobServiceClient(_settingsMonitor.CurrentValue.ConnectionString);

        var containers = blobServiceClient.GetBlobContainers();

        return containers.All(q => q.Name != containerName) ?
            blobServiceClient.CreateBlobContainer(containerName) : new BlobContainerClient(_settingsMonitor.CurrentValue.ConnectionString, containerName);
    }
}

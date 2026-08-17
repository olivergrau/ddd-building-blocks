using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Persistence.Storage;

public interface IStringStorageService
{
    Task SaveAsync(string content, string key, System.Threading.CancellationToken cancellationToken);
    Task<string?> GetAsync(string key, System.Threading.CancellationToken cancellationToken);
    Task DeleteAsync(string key, System.Threading.CancellationToken cancellationToken);
}

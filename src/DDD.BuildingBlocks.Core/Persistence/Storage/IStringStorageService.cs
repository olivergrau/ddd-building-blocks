using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Persistence.Storage;

public interface IStringStorageService
{
    Task SaveAsync(string content, string key);
    Task<string?> GetAsync(string key);
    Task DeleteAsync(string key);
}

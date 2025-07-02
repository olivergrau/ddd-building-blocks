using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Commanding
{
    public interface ICommandDispatcher
    {
        Task<CommandPublishResult> PublishAsync(Command command);
    }
}
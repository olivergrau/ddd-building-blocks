using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Commanding
{
    /// <summary>
    ///     Represents a handler for the command logic.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommandHandler<in T> where T : ICommand
    {
        /// <summary>
        ///     Handles the command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        Task HandleCommandAsync(T command);
    }
}

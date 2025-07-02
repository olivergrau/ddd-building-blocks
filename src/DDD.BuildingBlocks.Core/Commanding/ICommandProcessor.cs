using System;
using System.Threading.Tasks;

namespace DDD.BuildingBlocks.Core.Commanding
{
    /// <summary>
    ///     Executes a domain command.
    /// </summary>
    public interface ICommandProcessor
    {
        /// <summary>
        ///     Registers a factory function for a handler.
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="factoryMethod"></param>
        void RegisterHandlerFactory<TCommand>(Func<ICommandHandler<TCommand>> factoryMethod)
            where TCommand : class, ICommand;

        /// <summary>
        ///     Registers an instance of a handler.
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="handler"></param>
        void RegisterHandlerInstance<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : class, ICommand;

        /// <summary>
        ///     Executes a handler which has been registered before.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<CommandExecutionResult> ExecuteAsync<T>(T command) where T : class, ICommand;

        event EventHandler<ErrorEventArgs> OnError;
    }
}

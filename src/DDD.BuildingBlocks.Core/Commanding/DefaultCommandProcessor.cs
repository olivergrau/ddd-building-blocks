using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DDD.BuildingBlocks.Core.Exception;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace DDD.BuildingBlocks.Core.Commanding
{
    using System.Globalization;

    /// <summary>
    ///     Default implementation for a domain command processor which registers handlers and executes them.
    /// </summary>
    /// <inheritdoc cref="ICommandProcessor" />
    public class DefaultCommandProcessor : ICommandProcessor
    {
        private readonly ILogger? _log;

        private readonly Dictionary<Type, Func<object, Task>> _instanceRoutes;
        private readonly Dictionary<Type, Func<object>> _factoryRoutes;

        private readonly IEnumerable<Action<ICommand>> _preExecutionPipe;

        public DefaultCommandProcessor(
            IEnumerable<Action<ICommand>>? preExecutionPipe = null, ILoggerFactory? loggerFactory = null)
        {
            _preExecutionPipe = preExecutionPipe ?? [];
            _instanceRoutes = new Dictionary<Type, Func<object, Task>>();
            _factoryRoutes = new Dictionary<Type, Func<object>>();

            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(DefaultCommandProcessor))
                : nullLoggerFactory.CreateLogger(nameof(DefaultCommandProcessor));

            nullLoggerFactory.Dispose();
        }

        public void RegisterHandlerFactory<TCommand>(Func<ICommandHandler<TCommand>> factoryMethod)
            where TCommand : class, ICommand
        {
            _factoryRoutes.Add(typeof(TCommand), factoryMethod);
        }

        public void RegisterHandlerInstance<TCommand>(ICommandHandler<TCommand> handler)
            where TCommand : class, ICommand
        {
            _instanceRoutes.Add(typeof(TCommand), command => handler.HandleCommandAsync(
                command as TCommand ?? throw new CommandCreationFailedException("Command handler instance could not be registered.")));
            _log!.LogDebug("Registered {Handler} instance", handler.GetType().FullName);
        }

        public async Task<CommandExecutionResult> ExecuteAsync<T>(T command) where T : class, ICommand
        {
            var commandType = typeof(T);

            RunPreExecutionPipe(command);

            if (_instanceRoutes.ContainsKey(commandType))
            {
                return await ExecuteHandlerInstanceAsync(command);
            }

            if (_factoryRoutes.ContainsKey(commandType))
            {
                return await InstantiateAndExecuteHandlerAsync(command);
            }

            return new CommandExecutionResult(false, string.Format(CultureInfo.InvariantCulture, CoreErrors.NoCommandHandlerFound, commandType),
                new CommandExecutionFailedException(string.Format(CultureInfo.InvariantCulture, CoreErrors.NoCommandHandlerFound, commandType)));
        }

        public event EventHandler<ErrorEventArgs>? OnError;

        private async Task<CommandExecutionResult> InstantiateAndExecuteHandlerAsync<T>(T command)
            where T : ICommand
        {
            try
            {
                var commandType = typeof(T);
                _log!.LogDebug("DefaultCommandProcessor: executing command handler factory method {Command}",
                    commandType.FullName);

                var factory = _factoryRoutes[commandType];
                var commandHandler = factory.Invoke();

                _log!.LogDebug("DefaultCommandProcessor: instantiated handler {Command} -> {Handler}",
                    commandType.FullName,
                    commandHandler.GetType().FullName);

                _log!.LogDebug("DefaultCommandProcessor: executing command {Command}", commandType.FullName);

                if (!(commandHandler is ICommandHandler<T> handler))
                {
                    return new CommandExecutionResult(false, "Command handler not of type ICommandHandler<T>.", null);
                }

                await handler.HandleCommandAsync(command);

                return new CommandExecutionResult(true, "", null);
            }
            catch (System.Exception? exception)
            {
                OnErrorRaised(new ErrorEventArgs(exception, CoreErrors.CommandExecutionFailed));

                _log!.LogError(exception, CoreErrors.CommandExecutionFailed);
                return new CommandExecutionResult(false, CoreErrors.CommandExecutionFailed, exception);
            }
        }

        protected virtual void OnErrorRaised(ErrorEventArgs args)
        {
            OnError?.Invoke(this, args);
        }

        private async Task<CommandExecutionResult> ExecuteHandlerInstanceAsync<T>(T command)
            where T : ICommand
        {
            try
            {
                var commandType = typeof(T);
                _log!.LogDebug("DefaultCommandProcessor: executing registered command handler instance {Command}",
                    commandType.FullName);
                await _instanceRoutes[commandType].Invoke(command);
                return new CommandExecutionResult(true, "", null);
            }
            catch (System.Exception? exception)
            {
                _log!.LogError(exception, CoreErrors.CommandExecutionFailed);
                return new CommandExecutionResult(false, CoreErrors.CommandExecutionFailed, exception);
            }
        }

        private void RunPreExecutionPipe(ICommand command)
        {
            foreach (var action in _preExecutionPipe)
            {
                _log!.LogDebug("PreExecutionPipe: Executing action: {PreExecutionAction}", action.Method.Name);
                action(command);
            }
        }
    }
}

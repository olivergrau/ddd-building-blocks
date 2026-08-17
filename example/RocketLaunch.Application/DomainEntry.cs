using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.ErrorHandling;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Exception.Constants;
using System.Threading;

namespace RocketLaunch.Application
{
    public class DomainEntry : IDomainEntry
    {
        private readonly ICommandDispatcher _commandDispatcher;
        public DomainEntry(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }

        public async Task<ICommandExecutionResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
            where TCommand : DDD.BuildingBlocks.Core.Commanding.Command
        {
            var result = await _commandDispatcher.DispatchAsync(command, cancellationToken);

            if (result.IsSuccess || result.ResultException == null)
            {
                return result;
            }

            if (result.ResultException is ClassifiedErrorException ce)
            {
                return new CommandExecutionResult(false, ce.ErrorInfo.Message, ce);
            }

            var wrapped = result.ResultException switch
            {
                NotFoundException nf => new ClassifiedErrorException(
                    new ClassificationInfo(nf.Message, ErrorOrigin.ApplicationLevel, ErrorClassification.NotFound), nf),
                _ => new ApplicationProcessingException(HandlerErrors.ApplicationProcessingError, result.ResultException)
            };

            return new CommandExecutionResult(false, wrapped.ErrorInfo.Message, wrapped);
        }
    }
}

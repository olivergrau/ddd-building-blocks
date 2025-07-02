using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Commanding
{
    public class CommandPublishResult(bool isSuccess, string failReason, System.Exception ex) : ICommandPublishResult
    {
        public bool IsSuccess { get; } = isSuccess;
        public string FailReason { get; } = failReason;
        public System.Exception ResultException { get; } = ex;

        public void EnsurePublished()
        {
            if (IsSuccess == false)
            {
                throw new CommandExecutionFailedException(
                    $"Command failed with message: {FailReason} \n\n {ResultException?.Message}");
            }
        }
    }
}

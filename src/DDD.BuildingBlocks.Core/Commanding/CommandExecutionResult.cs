namespace DDD.BuildingBlocks.Core.Commanding
{
    public class CommandExecutionResult(bool isSuccess, string failReason, System.Exception? ex) : ICommandExecutionResult
    {
        public bool IsSuccess { get; } = isSuccess;
        public string FailReason { get; } = failReason;
        public System.Exception? ResultException { get; } = ex;
    }
}
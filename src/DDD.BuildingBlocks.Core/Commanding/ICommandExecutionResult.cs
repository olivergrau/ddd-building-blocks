namespace DDD.BuildingBlocks.Core.Commanding
{
    public interface ICommandExecutionResult
    {
        bool IsSuccess { get; }
        string FailReason { get; }
        System.Exception? ResultException { get; }
    }
}
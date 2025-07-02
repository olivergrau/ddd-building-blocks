namespace DDD.BuildingBlocks.Core.Commanding
{
    public interface ICommandPublishResult
    {
        bool IsSuccess { get; }
        string FailReason { get; }
        System.Exception ResultException { get; }
        void EnsurePublished();
    }
}

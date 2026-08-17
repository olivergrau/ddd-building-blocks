namespace DDD.BuildingBlocks.Core.Projection;

public enum ProjectionStatus
{
    Idle = 0,
    Running = 1,
    Faulted = 2,
    Rebuilding = 3,
}

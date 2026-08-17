using System.Data.Common;

namespace DDD.BuildingBlocks.Core.Projection;

public sealed record ProjectionTransactionContext(DbConnection? Connection, DbTransaction? Transaction)
{
    public static ProjectionTransactionContext None { get; } = new(null, null);
}

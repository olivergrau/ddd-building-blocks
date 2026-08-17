using System;
using System.Text.Json;
using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using DDD.BuildingBlocks.Tests.Abstracts.Snapshot;
using FluentAssertions;
using Xunit;

namespace DDD.BuildingBlocks.Core.Tests.Unit;

public sealed class SystemTextJsonSnapshotCodecShould
{
    [Fact]
    public void Round_trip_a_registered_snapshot()
    {
        var codec = TestSnapshotCodec.Create();
        var snapshot = new OrderSnapshot(
            "order-1", 7, DateTime.UtcNow, "Name", "Description", [], OrderState.Open, "P", "42");

        var decoded = codec.Decode(codec.Encode(snapshot, typeof(Order).FullName!));

        var orderSnapshot = decoded.Should().BeOfType<OrderSnapshot>().Subject;
        orderSnapshot.SerializedAggregateId.Should().Be("order-1");
        orderSnapshot.Version.Should().Be(7);
        orderSnapshot.Name.Should().Be("Name");
        orderSnapshot.Description.Should().Be("Description");
    }

    [Theory]
    [InlineData("unknown.snapshot", 1)]
    [InlineData("order.snapshot", 2)]
    public void Discard_an_unknown_or_incompatible_snapshot(string snapshotType, int schemaVersion)
    {
        var codec = TestSnapshotCodec.Create();
        using var payload = JsonDocument.Parse("{}");
        var envelope = new SnapshotEnvelope(
            "order-1", typeof(Order).FullName!, 7, snapshotType, schemaVersion, DateTimeOffset.UtcNow, payload.RootElement);

        codec.Decode(envelope).Should().BeNull();
    }
}

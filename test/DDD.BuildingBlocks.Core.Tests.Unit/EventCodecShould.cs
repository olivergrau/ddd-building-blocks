using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Event.Serialization;
using DDD.BuildingBlocks.Core.Exception;
using FluentAssertions;
using Xunit;

namespace DDD.BuildingBlocks.Core.Tests.Unit;

public sealed class EventCodecShould
{
    private const string EventType = "profiles.name-changed";

    [Fact(DisplayName = "Round-trip a domain payload without persisting CLR metadata")]
    [Trait("Category", "Unittest")]
    public void Round_trip_a_domain_payload_without_persisting_clr_metadata()
    {
        var registry = new EventTypeRegistry().Register<ProfileNameChanged>(EventType, 2);
        var codec = new SystemTextJsonEventCodec(registry);
        var domainEvent = new ProfileNameChanged("New name");
        var metadata = CreateMetadata(streamVersion: 7);

        var envelope = codec.Encode(domainEvent, metadata);
        var decoded = codec.Decode(envelope).Should().BeOfType<ProfileNameChanged>().Subject;

        envelope.EventType.Should().Be(EventType);
        envelope.SchemaVersion.Should().Be(2);
        envelope.GlobalPosition.Should().BeNull();
        envelope.Payload.TryGetProperty("displayName", out _).Should().BeTrue();
        envelope.Payload.TryGetProperty("fullType", out _).Should().BeFalse();
        envelope.Payload.TryGetProperty("targetVersion", out _).Should().BeFalse();
        envelope.Payload.TryGetProperty("serializedAggregateId", out _).Should().BeFalse();
        decoded.DisplayName.Should().Be("New name");
        decoded.SerializedAggregateId.Should().Be(metadata.StreamId);
        decoded.TargetVersion.Should().Be(6);
        decoded.ClassVersion.Should().Be(2);
        decoded.CorrelationId.Should().Be(metadata.CorrelationId);
    }

    [Fact(DisplayName = "Upcast a historical JSON fixture one schema version at a time")]
    [Trait("Category", "Unittest")]
    public void Upcast_a_historical_json_fixture_one_schema_version_at_a_time()
    {
        var registry = new EventTypeRegistry().Register<ProfileNameChanged>(EventType, 2);
        var codec = new SystemTextJsonEventCodec(registry, [new RenameProfileNameUpcaster()]);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "profile-name-changed-v1.json");
        using var fixture = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var envelope = CreateEnvelope(fixture.RootElement, schemaVersion: 1);

        var decoded = codec.Decode(envelope).Should().BeOfType<ProfileNameChanged>().Subject;

        decoded.DisplayName.Should().Be("Historical name");
        decoded.ClassVersion.Should().Be(2);
    }

    [Fact(DisplayName = "Reject an unknown stable event type")]
    [Trait("Category", "Unittest")]
    public void Reject_an_unknown_stable_event_type()
    {
        var codec = new SystemTextJsonEventCodec(new EventTypeRegistry());
        using var payload = JsonDocument.Parse("{}");
        var envelope = CreateEnvelope(payload.RootElement, schemaVersion: 1);

        Action decode = () => codec.Decode(envelope);

        decode.Should().Throw<UnknownEventTypeException>()
            .Which.EventType.Should().Be(EventType);
    }

    [Fact(DisplayName = "Reject a historical schema when its next upcaster is missing")]
    [Trait("Category", "Unittest")]
    public void Reject_a_historical_schema_when_its_next_upcaster_is_missing()
    {
        var registry = new EventTypeRegistry().Register<ProfileNameChanged>(EventType, 2);
        var codec = new SystemTextJsonEventCodec(registry);
        using var payload = JsonDocument.Parse("{\"name\":\"Historical name\"}");
        var envelope = CreateEnvelope(payload.RootElement, schemaVersion: 1);

        Action decode = () => codec.Decode(envelope);

        decode.Should().Throw<UnsupportedEventSchemaVersionException>()
            .Which.SchemaVersion.Should().Be(1);
    }

    [Fact(DisplayName = "Reject duplicate stable keys and duplicate CLR mappings")]
    [Trait("Category", "Unittest")]
    public void Reject_duplicate_stable_keys_and_duplicate_clr_mappings()
    {
        var registry = new EventTypeRegistry().Register<ProfileNameChanged>(EventType, 2);

        Action duplicateKey = () => registry.Register<OtherProfileEvent>(EventType);
        Action duplicateClrType = () => registry.Register<ProfileNameChanged>("profiles.renamed", 2);

        duplicateKey.Should().Throw<EventTypeRegistrationException>();
        duplicateClrType.Should().Throw<EventTypeRegistrationException>();
    }

    [Fact(DisplayName = "Represent stream versions beyond the 32-bit range")]
    [Trait("Category", "Unittest")]
    public void Represent_stream_versions_beyond_the_32_bit_range()
    {
        var targetVersion = (long)int.MaxValue + 10;
        var domainEvent = new LargeVersionEvent(targetVersion);

        domainEvent.TargetVersion.Should().Be(targetVersion);
        CreateMetadata(targetVersion + 1).StreamVersion.Should().Be(targetVersion + 1);
    }

    private static EventEnvelopeMetadata CreateMetadata(long streamVersion)
    {
        return new EventEnvelopeMetadata(
            Guid.NewGuid(),
            "profile-42",
            "profile",
            streamVersion,
            null,
            DateTimeOffset.Parse("2026-08-16T10:00:00Z"),
            CorrelationId: "correlation-1",
            CausationId: "cause-1",
            CommandId: "command-1",
            Actor: "user-1",
            TurnId: "turn-1");
    }

    private static EventEnvelope CreateEnvelope(JsonElement payload, int schemaVersion)
    {
        var metadata = CreateMetadata(streamVersion: 7);
        return new EventEnvelope(
            metadata.EventId,
            metadata.StreamId,
            metadata.AggregateType,
            metadata.StreamVersion,
            metadata.GlobalPosition,
            EventType,
            schemaVersion,
            metadata.OccurredAt,
            metadata.CommittedAt,
            metadata.CorrelationId,
            metadata.CausationId,
            metadata.CommandId,
            metadata.Actor,
            metadata.TurnId,
            payload);
    }

    private sealed class ProfileNameChanged(string displayName) : DomainEvent("profile-42", 6, 2)
    {
        public string DisplayName { get; } = displayName;
    }

    private sealed class OtherProfileEvent() : DomainEvent("profile-42", -1, 1);

    private sealed class LargeVersionEvent(long targetVersion) : DomainEvent("profile-42", targetVersion, 1);

    private sealed class RenameProfileNameUpcaster : IEventUpcaster
    {
        public string EventType => EventCodecShould.EventType;
        public int SourceSchemaVersion => 1;
        public int TargetSchemaVersion => 2;

        public JsonNode Upcast(JsonNode payload)
        {
            var source = payload.AsObject();
            source["displayName"] = source["name"]?.DeepClone();
            source.Remove("name");
            return source;
        }
    }
}

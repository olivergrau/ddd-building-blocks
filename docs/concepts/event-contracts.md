# Event contracts and evolution

## Envelope and payload

`EventEnvelope` separates storage metadata from the domain-event JSON payload. The envelope carries event ID, stream identity and version, aggregate type, global position, stable event type, schema version, timestamps, and optional correlation/causation context.

The codec removes legacy event metadata properties from the payload when encoding and reconstructs them from the envelope when decoding. Provider schemas therefore do not depend on CLR type names or a serialized domain-event base class.

## Stable event keys

Each persisted event has one explicit key:

```csharp
var registry = new EventTypeRegistry()
    .Register<MissionCreated>("mission.created")
    .Register<MissionScheduled>("mission.scheduled");
```

Stable keys are permanent contracts. Follow these rules:

- do not use `Type.FullName` or assembly-qualified names;
- do not encode a C# namespace that may be reorganized;
- choose a bounded-context-oriented naming convention;
- never reuse a retired key for a different semantic fact;
- fail startup on duplicate keys or duplicate CLR registrations.

## Stable aggregate keys

Streams also require a stable aggregate type key. Register aggregate CLR types independently from event types:

```csharp
var aggregateTypes = new AggregateTypeRegistry()
    .Register<Mission>("lunar-ops.mission")
    .Register<CrewMember>("lunar-ops.crew-member");
```

Pass the registry to `EventSourcingRepository.Create`. The key becomes part of stream identity and must remain unchanged when namespaces, assemblies, or CLR type names are refactored. Duplicate keys, duplicate CLR registrations, and attempts to use an unregistered aggregate fail explicitly.

The repository constructor without an aggregate registry remains available for 2.0 compatibility and derives aggregate types from CLR full names. New durable applications should use explicit registration.

## Commit metadata

Keep transport and workflow identifiers outside domain-event payloads. Supply them when the aggregate is saved:

```csharp
await repository.SaveAsync(
    mission,
    new EventCommitMetadata(
        CorrelationId: request.CorrelationId,
        CausationId: request.CausationId,
        CommandId: request.CommandId,
        Actor: request.Actor,
        TurnId: request.TurnId),
    cancellationToken);
```

The repository copies the same commit metadata to every event produced by that aggregate operation. If `CorrelationId` is omitted, the correlation ID already captured by each domain event is preserved. Commit metadata supports tracing and application-level idempotency; it does not itself provide a command inbox or turn store.

## Schema versions

Every registration has a positive current schema version. The event class's `ClassVersion` must match it when encoding.

Additive changes are not automatically harmless: constructor requirements, value-object formats, enum representations, and serializer settings can still break historical payloads. Preserve a fixture for every historical schema you must read.

## Upcasting

An `IEventUpcaster` transforms JSON from exactly one schema version to the next:

```csharp
public sealed class MissionCreatedV1ToV2 : IEventUpcaster
{
    public string EventType => "mission.created";
    public int SourceSchemaVersion => 1;
    public int TargetSchemaVersion => 2;

    public JsonNode Upcast(JsonNode payload)
    {
        var value = payload.AsObject();
        value["priority"] = "normal";
        return value;
    }
}
```

Register the chain with the codec:

```csharp
var codec = new SystemTextJsonEventCodec(
    registry,
    new IEventUpcaster[] { new MissionCreatedV1ToV2() });
```

Each upcaster must advance one version. A `v1 -> v3` shortcut is rejected. Sequential steps make every historical transition explicit and testable.

## Failure behavior

Decoding fails explicitly when:

- the stable key is unknown;
- stored schema is newer than the application understands;
- an upcaster step is missing or duplicated;
- JSON is malformed or cannot construct the event;
- stream metadata contradicts the domain event.

Do not skip an undecodable event. That would produce aggregate or projection state from incomplete history.

## Event design guidance

- Name facts in past tense.
- Store enough information to replay the decision's effect.
- Prefer domain values over database foreign keys without meaning.
- Avoid references to mutable external state that replay would have to query.
- Record correlation and causation metadata when tracing cross-process workflows.
- Treat deletion as a domain event, not physical removal from history.

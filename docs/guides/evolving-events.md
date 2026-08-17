# Evolving event contracts

Persisted events outlive application deployments. Plan every change as a data-contract change, not merely a C# refactoring.

## Classify the change

Ask:

1. Does the event mean the same historical fact?
2. Can the current CLR type deserialize every stored payload?
3. Does replay produce the same domain effect?
4. Do projections interpret the event compatibly?

If the meaning changes, create a new event type/key. Upcasting is for representation evolution, not rewriting history into a different fact.

## Safe workflow

1. Preserve a real or representative old JSON fixture.
2. Increment the event class/schema version.
3. Keep the stable event key unchanged only if semantics remain unchanged.
4. Implement one `IEventUpcaster` for each version step.
5. Register all steps in `SystemTextJsonEventCodec`.
6. Test fixture-to-current decoding and aggregate replay.
7. Test affected projections and rebuilds.
8. Deploy readers that understand both old and new representations before writing the new version where rolling deployments require it.

## Example: add a required property

Suppose `mission.created` version 1 had no priority and version 2 requires it. The upcaster supplies the historical default:

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

The default must reflect what version-1 events meant at the time. Do not query current database state during upcasting.

## Renaming CLR types

You may rename or move an event CLR type while keeping its stable key, provided the registry maps that key to the new type and historical payloads remain compatible. This is the principal reason stable keys exist.

## Changing aggregate CLR type names

The repository uses aggregate full names as aggregate-type discriminators. Moving or renaming a persisted aggregate type requires a storage migration or an application-level compatibility strategy. Event stable keys alone do not migrate the aggregate-type column.

## Removing events

Do not remove a registration while stored envelopes use its key. A retired event may remain necessary forever for replay. If a bounded context is decommissioned, archive the reader code and migration/recovery procedure with its event history.

## Snapshot impact

Snapshots are disposable. When aggregate shape changes, increment snapshot schema version and let incompatible snapshots fall back to event replay. Avoid building an event-style snapshot upcaster system unless measurement proves that rebuilding snapshots is impractical.

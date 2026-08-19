# DDD.BuildingBlocks 2.1.0

DDD.BuildingBlocks 2.1.0 is an additive stable release that makes durable aggregate identities and workflow metadata explicit.

## Highlights

- Explicit stable aggregate type keys through `AggregateTypeRegistry`.
- Stream identity remains stable across CLR namespace, assembly, and type-name refactorings.
- Complete event commit metadata for correlation, causation, command, actor, and turn identifiers.
- Compatibility paths for existing 2.0 repository construction and saves.
- Updated quickstart, event-contract, and composition-root documentation.

## Stable aggregate identities

New durable applications should construct the repository with explicitly registered logical aggregate keys:

```csharp
var aggregateTypes = new AggregateTypeRegistry()
    .Register<Session>("playground.session")
    .Register<Persona>("playground.persona");

var repository = EventSourcingRepository.Create(
    eventStore,
    eventCodec,
    aggregateTypes);
```

The existing constructor remains available for compatibility and continues to derive aggregate types from CLR full names.

## Commit metadata

Applications can keep workflow and transport identifiers outside domain-event payloads:

```csharp
await repository.SaveAsync(
    aggregate,
    new EventCommitMetadata(
        CorrelationId: correlationId,
        CausationId: causationId,
        CommandId: commandId,
        Actor: actor,
        TurnId: turnId),
    cancellationToken);
```

The metadata is copied to every envelope in the aggregate commit. This supports tracing and application-level deduplication; applications still own command inboxes and operational turn stores.

## Compatibility

This release does not change provider schemas or the event, snapshot, and projection storage formats. Existing 2.0 consumers can upgrade without changing their current repository construction. Adopt stable aggregate keys before writing new durable streams when refactoring-safe aggregate identity is required.

## Distribution

The GitHub Release contains seven `.nupkg` files, seven `.snupkg` symbol packages, release notes, the complete change inventory, the 2.0 migration guide, license, archive, and SHA-256 checksums. Packages are not published to NuGet.org.

## Known limitation

The legacy RocketLaunch API example retains its known asynchronous in-memory projection timing race. The deterministic projection runner and production checkpoint stores are unaffected.

# DDD.BuildingBlocks – F2 tactical domain primitives and Gate G2

As of: August 16, 2026  
Branch: `modernization-2026`  
Goal: harden tactical domain contracts while retaining the framework's event-sourcing focus

## Architecture decision

- `AggregateRoot<TKey>` remains the only aggregate-root base type and is explicitly event-sourced.
- No conventional or plain aggregate-root base class and no marker-only aggregate-root interface are introduced.
- All genuine Playground domain aggregates use event sourcing. Relational persistence is reserved for projections and non-aggregate technical or operational state.
- The existing type name is retained because a second aggregate-root model no longer exists to disambiguate.

## Public API inventory

| Primitive | Public/protected contract | Semantics and assumptions | Known consumers |
|---|---|---|---|
| `Entity<TKey>` | constructor, `Id`, `SerializedId`, equality and hash code | Identity is a typed `EntityId`; equality requires the same runtime entity type and equal ID. `SerializedId` is the persistence-facing string representation. | All example and test aggregates and child entities |
| `EntityId<T>` | value-object base type | Identifier structure and validation belong to the concrete ID. Equality follows its declared value attributes. | Aggregate IDs in LunarOps, RocketLaunch, and provider-contract tests |
| `ValueObject<T>` | equality, `==`, `!=`, hash code; protected equality attributes | Structural equality is ordered and null-safe. Concrete types control the participating attributes. | IDs and domain values throughout examples and tests |
| `AggregateRoot<TKey>` | versions, stream state, replay, commit tracking, uncommitted changes; protected `RaiseEvent`, compatibility `ApplyEvent`, deactivation and ID reconstruction | New events mutate state once and enter the pending list; replay mutates state without creating pending events. Handler discovery uses attributed methods and is validated once per aggregate type. | All LunarOps and RocketLaunch aggregates plus provider-contract aggregates |
| `IEventSourcingBasedAggregate` | serialized ID, versions, stream state, pending-change access, replay and commit marking | Persistence-facing compatibility contract. No new abstract interface member was added in F2. | Generic repository and storage providers |
| `DomainEvent` / `IDomainEvent` | aggregate ID, target version, timestamps, correlation, class and CLR type metadata | Existing mutable persistence contract remains intact for compatibility. Stable logical event identity and envelopes belong to F3. | All events, repositories, providers, handlers, and examples |
| Aggregate exceptions | typed exception classes carrying aggregate context where applicable | Missing handlers and duplicate handler declarations now fail with classified aggregate exceptions rather than incidental lookup or generic exceptions. | Aggregate execution and repository error paths |

The domain primitives do not reference ASP.NET Core, EF Core, Npgsql, HTTP, or hosting APIs. F2 adds no infrastructure or serializer dependency. The legacy string serialization boundary and CLR event type metadata are intentionally handled as part of the F3 event-envelope work rather than being partially redesigned here.

## Implemented behavior

- Added `RaiseEvent(IDomainEvent)` to express creation and application of a new event.
- Retained protected `ApplyEvent(IDomainEvent)` as a source- and binary-compatible forwarding alias.
- Migrated all in-repository aggregate consumers to `RaiseEvent`.
- Added a read-only snapshot property for pending events; the existing interface method remains compatible and returns the same protected view.
- Rejected replay on aggregates that already contain pending changes.
- Ensured a missing handler is detected before first-event ID mutation.
- Detect duplicate handlers during aggregate metadata initialization with `AggregateEventHandlerConfigurationException`.
- Made entity equality require matching runtime types as well as matching typed IDs.
- Made value-object equality and hashing safe for null equality attributes.
- Verified that storage commit failures leave pending events and committed-version state intact.

## Version boundary

`NoStream` remains `-1`; the first successfully applied event moves the aggregate to version `0`. An event's `TargetVersion` is the version against which it applies, while `CurrentVersion` is the last applied event version and `LastCommittedVersion` is the last replayed or successfully marked committed version.

At the F2 gate, the public contracts still used `int`. Moving only the aggregate root to `long` would have created an inconsistent API because commands, events, snapshots, repository/provider interfaces, provider implementations, and SQL schemas participate in the same version contract. F2 therefore recorded this complete migration boundary. The coordinated breaking conversion was subsequently implemented in F3 after Gate G2 approval.

## Compatibility assessment

- No public or protected member was removed or renamed.
- `AggregateRoot<TKey>` remains unchanged as the consumer base type.
- Existing `ApplyEvent` consumers continue to compile; `RaiseEvent` is additive.
- `IEventSourcingBasedAggregate` did not gain a mandatory member, preserving custom implementations.
- Behavioral changes are intentional F2 hardening: cross-runtime-type entities no longer compare equal, replay with pending events is rejected, and duplicate handler declarations fail early with a classified exception.
- Microsoft `ApiCompat` reports no breaking API changes against the approved F1 assembly.

## Verification

- Solution build: successful with 0 warnings and 0 errors.
- Core: 63/63 passed.
- DevelopmentPackage integration: 22/22 passed.
- MSSQL integration: 33 passed, 2 intentionally skipped, 0 failed.
- RocketLaunch Domain: 21/21 passed.
- LunarOps Domain: 53/53 passed.
- RocketLaunch Application: 23/23 passed.
- RocketLaunch ReadModel: 27/27 passed.
- API: 5/7 passed. The two failures are the asynchronous projection race already documented and accepted at Gate G1; F2 did not alter projection processing.
- Package build: exactly six `.nupkg` and six `.snupkg` artifacts, without warnings.

## Gate G2

F2 is implemented and **ready for review at Gate G2**. No provider conversion or F3 event-envelope work has been started. Explicit Gate G2 approval is required before proceeding.

# DDD.BuildingBlocks – F3 event contracts and evolution and Gate G3

As of: August 16, 2026
Branch: `modernization-2026`
Goal: define stable, evolvable event persistence contracts without coupling stored records to CLR identity

## Implemented contracts

- `EventEnvelope` is an immutable provider-neutral record containing event identity, stream identity and version, optional global position, stable event type, schema version, timestamps, correlation and causation metadata, optional command/actor/turn metadata, and an immutable JSON payload.
- `GlobalPosition` is nullable before persistence. Only a production provider may assign it during commit; domain events do not carry store positions.
- `EventTypeRegistry` explicitly maps one stable string key to exactly one current CLR event type. Duplicate stable keys and duplicate CLR mappings fail immediately.
- `IEventCodec` separates providers from serialization details.
- `SystemTextJsonEventCodec` is the default codec. It stores domain payload separately from technical envelope metadata and does not persist assembly-qualified names in new envelope records.
- Unknown event keys, unsupported schema versions, registration conflicts, and serialization failures have dedicated exception types.
- `IEventUpcaster` advances JSON payloads exactly one schema version at a time. The codec follows the complete registered chain and never rewrites historical records.

## Stable identity and schema rules

- Stable event keys are explicit, case-sensitive strings such as `profiles.name-changed`; CLR names are not fallback persistence keys.
- Schema versions start at `1` and are independent of package or assembly versions.
- Each upcaster must advance from version `n` to `n + 1`.
- Missing steps and future schema versions fail as `UnsupportedEventSchemaVersionException`.
- The registry's current schema version is authoritative when encoding new records.
- Historical compatibility is protected by checked-in JSON fixtures.

## Version migration

The coordinated version boundary identified in F2 is now migrated from `int` to `long` across:

- commands and domain events;
- aggregate current and committed versions;
- snapshots and snapshot services;
- repository and event-storage interfaces;
- in-memory and MSSQL provider implementations;
- example events, snapshots, and test contracts.

The legacy MSSQL DDL uses `BIGINT` for aggregate, event, and snapshot versions. Values beyond the 32-bit range are covered by a focused unit test.

This is an intentional public breaking change. Microsoft `ApiCompat` reports only the expected removals/additions caused by changing version-bearing members and parameters from `int` to `long`. A release containing F3 must use an appropriate SemVer major version.

## Legacy persistence boundary

The existing MSSQL provider continues to read and write its legacy Newtonsoft/CLR-oriented event format, apart from the coordinated `BIGINT` version change. It is not silently switched to the new envelope format before Gate G3. New production-provider records must use the envelope, registry, and codec contract and must not persist assembly-qualified event names.

There is no known production MSSQL event inventory to preserve. F3 therefore provides no speculative universal migration. If such an inventory is identified before provider adoption, it requires an explicit migration decision and fixture set.

## Verification

- Solution build: successful with 0 warnings and 0 errors.
- Core: 69/69 passed, including codec, registry, envelope, upcasting, historical fixture, failure classification, and 64-bit version tests.
- DevelopmentPackage integration: 22/22 passed.
- MSSQL integration against the `BIGINT` schema: 33 passed, 2 intentionally skipped, 0 failed.
- RocketLaunch Domain: 21/21 passed.
- LunarOps Domain: 53/53 passed.
- RocketLaunch Application: 23/23 passed.
- RocketLaunch ReadModel: 27/27 passed.
- API: 6/7 passed. The single failure is the asynchronous projection race documented and accepted at Gate G1; F3 does not change projection synchronization.
- Package build: exactly six `.nupkg` and six `.snupkg` artifacts, without warnings.

## Gate G3

F3 is implemented and **ready for review at Gate G3**. Provider adoption of the new persistence format and F4 async/cancellation/error/DI changes have not started. Explicit Gate G3 approval is required before proceeding.

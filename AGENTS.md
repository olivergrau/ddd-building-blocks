# DDD.BuildingBlocks contribution rules

## Repository language

- Write all repository content in English, including source code identifiers, comments, tests, documentation, configuration descriptions, and user-facing text.
- German may be used in chat, but must not be added to repository files.

## Current modernisation status

- Branch: `modernization-2026`.
- F0 is complete and its gate was approved.
- F1 is implemented, verified, and approved at Gate G1.
- F2 is implemented, verified, and approved at Gate G2.
- F3 is implemented, verified, and approved at Gate G3.
- F4 is implemented, verified, and approved at Gate G4.
- F5 is implemented, verified, and approved at Gate G5.
- F6 selected PostgreSQL with Npgsql and explicit SQL; Gate G6 was approved.
- F7 is implemented, verified, and approved at Gate G7.
- F7.4 SQL Server parity provider is implemented, verified, and accepted for continuation.
- F8 is implemented, verified, and approved at Gate G8.
- F9 is implemented, verified, and approved at Gate G9.
- F10 is implemented, verified, and published as stable GitHub Release `v2.0.0` from main commit `4550cba`.
- Agreed F2 direction: keep the existing `AggregateRoot<TKey>` name because the framework supports only event-sourced domain aggregates.
- Do not introduce `ConventionalAggregateRoot<TKey>` or `PlainAggregateRoot<TKey>`; DDD.BuildingBlocks remains focused on event-sourced domain aggregates.
- Do not introduce a marker-only `IAggregateRoot<TKey>` without a concrete consumer.
- Playground domain aggregates use event sourcing; relational persistence is reserved for projections and non-aggregate technical or operational state.

## Completed work

- Pinned the stable .NET SDK `10.0.400`; all projects target `net10.0` with C# 14.
- Added central build settings, warnings-as-errors, analyzers, nullable, deterministic builds, Source Link, XML documentation, and symbol packages.
- Added Central Package Management and updated stable dependencies within the F1 risk boundary.
- Restricted packaging to the seven active `src` packages through `DDD.BuildingBlocks.Packages.slnf`.
- Restored test discovery for the RocketLaunch and LunarOps domain tests.
- Removed the obsolete runtime roll-forward workaround.
- Verified public API compatibility against F0 commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`; no breaking changes were found.
- Organized and translated all specifications into English.
- Hardened tactical domain primitives: runtime-type-aware entity equality, null-safe value-object equality, explicit `RaiseEvent` semantics, protected replay boundaries, immutable uncommitted-event snapshots, and validated event-handler metadata.
- Preserved the public `AggregateRoot<TKey>` and `ApplyEvent` APIs; the latter remains as a compatibility alias while known consumers use `RaiseEvent`.
- Documented the coordinated post-G2 migration boundary from `int` to `long` stream versions across event, snapshot, provider, and storage contracts.
- Added the immutable event envelope, explicit stable-key registry, `IEventCodec`, default `SystemTextJsonEventCodec`, sequential JSON upcasting, historical fixtures, and classified registry/codec errors.
- Migrated stream-related versions to `long` across commands, events, aggregates, snapshots, repository/provider contracts, examples, and tests.
- Removed `ServiceLocator`, `IDependencyResolver`, `DefaultCommandProcessor`, and their compatibility APIs completely.
- Added validated startup registration plus scope-safe command and domain-event dispatch; each dispatch creates an independent DI scope.
- Added mandatory `CancellationToken` parameters to asynchronous framework I/O and dispatch contracts, propagated cancellation through providers, and kept cancellation exceptions unwrapped.
- Expanded the shared error taxonomy with the F4 domain, stream, codec, provider, and cancellation categories.
- Added the envelope-based `IEventStoreProvider` contract for atomic expected-version append, stream reads, and the committed global feed.
- Added a thread-safe normative in-memory event store with unique event IDs, stable stream ordering, monotone global positions, immutable stored payloads, and classified conflicts.
- Kept projection publication outside the provider contract through a post-commit `PublishingEventStoreProviderDecorator`.
- Added the abstract provider-contract suite that future production providers must pass unchanged.
- Removed `IEventStorageProvider`, both legacy Development providers, and the non-conforming MSSQL persistence implementation and integration suite.
- Migrated `EventSourcingRepository`, RocketLaunch, and repository tests to `IEventStoreProvider` plus explicit `IEventCodec` registration.
- Removed provider-reflection-based `UniqueDomainPropertyAttribute`; cross-stream uniqueness must be modeled explicitly outside a generic event store.
- Added the immutable versioned snapshot envelope, stable-key registry, JSON codec, and normative `ISnapshotStoreProvider` contract.
- Added mandatory snapshot-provider implementations for In-Memory, PostgreSQL, and SQL Server while keeping consumer activation optional and full replay available.
- Made incompatible snapshots disposable and isolated snapshot failures from successful event commits; removed the legacy CLR-object and file-dump snapshot providers.
- Set a coordinated stable `2.0.0` package version with MIT license, repository/release metadata, embedded README, Source Link, XML documentation, and symbols.
- Added the extensive 1.x-to-2.x `CHANGES.md`, ordered migration guide, stable release notes, package inspection, clean local-feed consumer smoke test, checksums, and tag-triggered GitHub Release workflow.
- Distribution uses GitHub Release assets and a local NuGet source; no public registry publication or package signing is configured.
- Added the production PostgreSQL provider with Npgsql and explicit set-based SQL; EF Core is excluded from the event-store hot path.
- Added an idempotent versioned PostgreSQL schema, atomic stream locking, batched envelope append, and a checkpoint-safe transactional global-position allocator.
- Verified the PostgreSQL provider through the unchanged contract suite, real concurrency, large batches, forced-session rollback, and `pg_dump`/`pg_restore` in PostgreSQL 18 Testcontainers.
- Replaced the empty MSSQL placeholder with a clean-room SQL Server parity provider using Microsoft.Data.SqlClient, explicit locking SQL, and table-valued event batches without EF Core.
- Verified SQL Server through the unchanged provider contract, real concurrency, large TVP batches, forced-session rollback, and native backup/restore in SQL Server 2025 CU7 Testcontainers.
- Added projection identity, checkpoint, transaction-context, runner, retry, failure, reset, and rebuild contracts in Core.
- Added transactional projection checkpoint stores for In-Memory, PostgreSQL, and SQL Server; relational callbacks share the checkpoint connection and transaction.
- Added a seven-case checkpoint-store contract plus runner and relational atomicity tests covering idempotency, poison events, independent progress, retry, and deterministic rebuild.

## Verification status

- Restore succeeds.
- Build succeeds with zero warnings and zero errors.
- Core: 76 passed.
- DevelopmentPackage integration: 41 passed.
- SQL Server integration: 30 passed.
- RocketLaunch Domain: 21 passed; LunarOps Domain: 53 passed.
- RocketLaunch Application: 23 passed; ReadModel: 27 passed.
- PostgreSQL integration: 30 passed.
- Package build targets seven active packages.
- Package vulnerability scan, API compatibility check, consumer smoke test, and manual API smoke test pass.
- Known pre-existing issue: the API integration suite is flaky because it does not deterministically wait for asynchronous projection updates. The latest F1 run passed 5 of 7 tests; this was explicitly accepted for Gate G1 review.
- Latest F2 full run: every non-API suite passed; API remained at 5/7 with the same accepted projection race.
- F2 API compatibility against the approved F1 assembly passes with no breaking changes.
- Latest F3 full run: every non-API suite passed; API passed 6/7 with the same accepted projection race.
- Latest F4 run: build passed with zero warnings; Core passed 64/64, DevelopmentPackage 22/22, MSSQL 33 passed and 2 skipped, and API passed 6/7 with the same accepted projection race.
- Latest F5 run after legacy-provider removal: build passed with zero warnings; Core passed 64/64, DevelopmentPackage 28/28, and API passed 5/7 with the same accepted projection race. No MSSQL suite remains.
- Latest F7 run: build passed with zero warnings; PostgreSQL passed 16/16, every non-API suite passed, and API passed 5/7 with the same accepted projection race.
- Latest F7.4 parity run: build passed with zero warnings; PostgreSQL and SQL Server each passed 16/16, every other non-API suite passed, and API passed 6/7 with the same accepted projection race.
- Latest F8 run: build passed with zero warnings; Core passed 68/68, DevelopmentPackage 35/35, PostgreSQL and SQL Server each passed 24/24, every other non-API suite passed, and API passed 5/7 with the same accepted legacy projection race.
- Latest F9 run: build passed with zero warnings; Core passed 73/73, DevelopmentPackage 41/41, PostgreSQL and SQL Server each passed 30/30, every other non-API suite passed, and API passed 5/7 with the same accepted legacy projection race.
- Latest F10 Release run: build passed with zero warnings; Core passed 73/73, DevelopmentPackage 41/41, PostgreSQL and SQL Server each passed 30/30, RocketLaunch Application 23/23, ReadModel 27/27, Domain 21/21, and LunarOps Domain 53/53. Package inspection, vulnerability scan, and clean consumer smoke test passed. The timing-sensitive legacy API example passed 6/7.
- Latest post-2.0 aggregate-key/commit-metadata run: build passed with zero warnings; Core passed 76/76, DevelopmentPackage 41/41, PostgreSQL and SQL Server each passed 30/30, and all non-API example suites passed. The accepted timing-sensitive API projection race remained at 5/7.
- GitHub Release workflow run `32023841069` passed every gate and published seven `.nupkg`, seven `.snupkg`, the complete archive, checksums, license, changes, and migration guide.
- F3 intentionally breaks the public version contract from `int` to `long`; a future release containing F3 requires an appropriate SemVer major version.
- F4 intentionally removes the legacy locator/processor APIs and changes async handler and persistence signatures; it therefore remains part of the same coordinated breaking release.

## Working references

- Specification index: `specs/README.md`.
- Detailed plan: `specs/10-DDD.BuildingBlocks-Detailed-Implementation-Plan.md`.
- F0 report: `specs/reports/13-DDD.BuildingBlocks-F0-Baseline-Report.md`.
- F1/G1 report: `specs/reports/14-DDD.BuildingBlocks-F1-Outcome-Report.md`.
- F2/G2 report: `specs/reports/15-DDD.BuildingBlocks-F2-Tactical-Domain-Primitives-Report.md`.
- F3/G3 report: `specs/reports/16-DDD.BuildingBlocks-F3-Event-Contracts-and-Evolution-Report.md`.
- F4/G4 report: `specs/reports/17-DDD.BuildingBlocks-F4-Async-Cancellation-Error-and-DI-Report.md`.
- F5/G5 report: `specs/reports/18-DDD.BuildingBlocks-F5-In-Memory-Provider-and-Contract-Suite-Report.md`.
- F6 decision: `specs/adr/001-postgresql-production-event-store.md`.
- F7/G7 report: `specs/reports/19-DDD.BuildingBlocks-F7-PostgreSQL-Production-Provider-Report.md`.
- F7.4 SQL Server report: `specs/reports/20-DDD.BuildingBlocks-F7.4-SQL-Server-Parity-Provider-Report.md`.
- F8/G8 report: `specs/reports/21-DDD.BuildingBlocks-F8-Projections-and-Recovery-Report.md`.
- F9/G9 report: `specs/reports/22-DDD.BuildingBlocks-F9-Versioned-Snapshots-Report.md`.
- F10/G10 report: `specs/reports/23-DDD.BuildingBlocks-F10-Packaging-and-Stable-Release-Report.md`.
- Stable release documents: `CHANGES.md`, `MIGRATION-2.0.md`, and `RELEASE_NOTES-2.0.0.md`.
- Added the hierarchical English `docs/` user-documentation package with getting started, concepts, tutorials, operational guides, and reference pages based on the executable examples. A later task will add MkDocs configuration; no `mkdocs.yml` exists yet.
- Added explicit stable aggregate-type registration and repository commit metadata (`CorrelationId`, `CausationId`, `CommandId`, `Actor`, and `TurnId`) for the Persona Simulation Playground integration; the CLR-name repository constructor remains as a 2.0 compatibility path.
- Prepared stable package version `2.1.0`, release notes, change inventory, generalized tag-release automation, release bundle inspection, clean-consumer smoke verification, and vulnerability scan.
- `.vscode/`, `specs/`, and `to_read/` are local-only ignored workspace directories and are intentionally not tracked in the remote repository.
- Standard commands: `dotnet restore DDD.BuildingBlocks.sln`, `dotnet build DDD.BuildingBlocks.sln --no-restore`, and `dotnet test DDD.BuildingBlocks.sln --no-build`.
- Package command: `dotnet pack DDD.BuildingBlocks.Packages.slnf --no-build --configuration Debug --output artifacts/packages`.

## Status maintenance

- Update this file after each phase, gate decision, significant architectural decision, or material change to known issues and verification results.
- Keep this status concise and remove stale statements instead of accumulating a chronological log.

## Standing contribution constraints

- Keep the framework independent of Playground product logic.
- Preserve public API and SemVer expectations; document intentional breaking changes.
- Keep provider semantics covered by the provider-contract suite.
- Keep domain primitives free of ASP.NET Core, EF Core, Npgsql, HTTP, and hosting dependencies.
- Run the documented baseline commands before changing framework semantics.
- Stop for review after each modernisation gate; do not combine unrelated stages.

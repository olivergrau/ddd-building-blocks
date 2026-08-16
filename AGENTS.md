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
- F4 is implemented and verified; Gate G4 is awaiting explicit approval.
- Agreed F2 direction: keep the existing `AggregateRoot<TKey>` name because the framework supports only event-sourced domain aggregates.
- Do not introduce `ConventionalAggregateRoot<TKey>` or `PlainAggregateRoot<TKey>`; DDD.BuildingBlocks remains focused on event-sourced domain aggregates.
- Do not introduce a marker-only `IAggregateRoot<TKey>` without a concrete consumer.
- Playground domain aggregates use event sourcing; relational persistence is reserved for projections and non-aggregate technical or operational state.

## Completed work

- Pinned the stable .NET SDK `10.0.400`; all projects target `net10.0` with C# 14.
- Added central build settings, warnings-as-errors, analyzers, nullable, deterministic builds, Source Link, XML documentation, and symbol packages.
- Added Central Package Management and updated stable dependencies within the F1 risk boundary.
- Restricted packaging to the six `src` projects through `DDD.BuildingBlocks.Packages.slnf`.
- Restored test discovery for the RocketLaunch and LunarOps domain tests.
- Removed the obsolete runtime roll-forward workaround.
- Verified public API compatibility against F0 commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`; no breaking changes were found.
- Organized and translated all specifications into English.
- Hardened tactical domain primitives: runtime-type-aware entity equality, null-safe value-object equality, explicit `RaiseEvent` semantics, protected replay boundaries, immutable uncommitted-event snapshots, and validated event-handler metadata.
- Preserved the public `AggregateRoot<TKey>` and `ApplyEvent` APIs; the latter remains as a compatibility alias while known consumers use `RaiseEvent`.
- Documented the coordinated post-G2 migration boundary from `int` to `long` stream versions across event, snapshot, provider, and storage contracts.
- Added the immutable event envelope, explicit stable-key registry, `IEventCodec`, default `SystemTextJsonEventCodec`, sequential JSON upcasting, historical fixtures, and classified registry/codec errors.
- Migrated stream-related versions to `long` across commands, events, aggregates, snapshots, repository/provider contracts, examples, and tests; the legacy MSSQL schema now uses `BIGINT`.
- Removed `ServiceLocator`, `IDependencyResolver`, `DefaultCommandProcessor`, and their compatibility APIs completely.
- Added validated startup registration plus scope-safe command and domain-event dispatch; each dispatch creates an independent DI scope.
- Added mandatory `CancellationToken` parameters to asynchronous framework I/O and dispatch contracts, propagated cancellation through providers, and kept cancellation exceptions unwrapped.
- Expanded the shared error taxonomy with the F4 domain, stream, codec, provider, and cancellation categories.

## Verification status

- Restore succeeds.
- Build succeeds with zero warnings and zero errors.
- Core: 64 passed.
- DevelopmentPackage integration: 22 passed.
- MSSQL integration: 33 passed, 2 skipped; SQL Server runs through Testcontainers.
- RocketLaunch Domain: 21 passed; LunarOps Domain: 53 passed.
- RocketLaunch Application: 23 passed; ReadModel: 27 passed.
- Package build produces six `.nupkg` and six `.snupkg` files.
- Package vulnerability scan, API compatibility check, consumer smoke test, and manual API smoke test pass.
- Known pre-existing issue: the API integration suite is flaky because it does not deterministically wait for asynchronous projection updates. The latest F1 run passed 5 of 7 tests; this was explicitly accepted for Gate G1 review.
- Latest F2 full run: every non-API suite passed; API remained at 5/7 with the same accepted projection race.
- F2 API compatibility against the approved F1 assembly passes with no breaking changes.
- Latest F3 full run: every non-API suite passed; API passed 6/7 with the same accepted projection race.
- Latest F4 run: build passed with zero warnings; Core passed 64/64, DevelopmentPackage 22/22, MSSQL 33 passed and 2 skipped, and API passed 6/7 with the same accepted projection race.
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

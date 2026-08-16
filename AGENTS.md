# DDD.BuildingBlocks contribution rules

## Repository language

- Write all repository content in English, including source code identifiers, comments, tests, documentation, configuration descriptions, and user-facing text.
- German may be used in chat, but must not be added to repository files.

## Current modernisation status

- Branch: `modernization-2026`.
- F0 is complete and its gate was approved.
- F1 is implemented, verified, and approved at Gate G1.
- The F2 architecture is decided; no F2 implementation has started yet.
- Do not start F2 implementation until the user explicitly requests it.
- Agreed F2 direction: rename the existing `AggregateRoot<TKey>` to `EventSourcedAggregateRoot<TKey>` as an intentional breaking change.
- Do not introduce `ConventionalAggregateRoot<TKey>` or `PlainAggregateRoot<TKey>`; DDD.BuildingBlocks remains focused on event-sourced domain aggregates.
- Do not introduce a marker-only `IAggregateRoot<TKey>` without a concrete consumer.
- Playground domain aggregates use event sourcing; relational persistence is reserved for projections and non-aggregate technical or operational state.
- The current F1 changes are not committed yet.

## Completed work

- Pinned the stable .NET SDK `10.0.400`; all projects target `net10.0` with C# 14.
- Added central build settings, warnings-as-errors, analyzers, nullable, deterministic builds, Source Link, XML documentation, and symbol packages.
- Added Central Package Management and updated stable dependencies within the F1 risk boundary.
- Restricted packaging to the six `src` projects through `DDD.BuildingBlocks.Packages.slnf`.
- Restored test discovery for the RocketLaunch and LunarOps domain tests.
- Removed the obsolete runtime roll-forward workaround.
- Verified public API compatibility against F0 commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`; no breaking changes were found.
- Organized and translated all specifications into English.

## Verification status

- Restore succeeds.
- Build succeeds with zero warnings and zero errors.
- Core: 57 passed.
- DevelopmentPackage integration: 22 passed.
- MSSQL integration: 33 passed, 2 skipped; SQL Server runs through Testcontainers.
- RocketLaunch Domain: 21 passed; LunarOps Domain: 53 passed.
- RocketLaunch Application: 23 passed; ReadModel: 27 passed.
- Package build produces six `.nupkg` and six `.snupkg` files.
- Package vulnerability scan, API compatibility check, consumer smoke test, and manual API smoke test pass.
- Known pre-existing issue: the API integration suite is flaky because it does not deterministically wait for asynchronous projection updates. The latest F1 run passed 5 of 7 tests; this was explicitly accepted for Gate G1 review.

## Working references

- Specification index: `specs/README.md`.
- Detailed plan: `specs/10-DDD.BuildingBlocks-Detailed-Implementation-Plan.md`.
- F0 report: `specs/reports/13-DDD.BuildingBlocks-F0-Baseline-Report.md`.
- F1/G1 report: `specs/reports/14-DDD.BuildingBlocks-F1-Outcome-Report.md`.
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

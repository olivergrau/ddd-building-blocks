# DDD.BuildingBlocks 2.0.0

DDD.BuildingBlocks 2.0.0 is the first stable release of the modernized event-sourcing framework and is marked **READY FOR PLAYGROUND**.

## Highlights

- .NET 10 LTS and C# 14 baseline.
- Explicit stable event keys, versioned JSON envelopes, and sequential upcasting.
- Atomic expected-version event stores with committed global feeds.
- Production PostgreSQL and SQL Server providers using explicit SQL without EF Core in the hot path.
- Transactional projection checkpoints, retry, reset, and deterministic rebuild.
- Versioned disposable snapshots for In-Memory, PostgreSQL, and SQL Server.
- Scope-safe command and domain-event dispatch using Microsoft.Extensions.DependencyInjection.
- Reusable provider-contract suites and real-database operational tests.
- Deterministic NuGet packages with Source Link, XML documentation, and symbol packages.

## Breaking release

Version 2.0 intentionally removes the 1.x storage, SQL Server, service-locator, and default-command-processor APIs. Stream versions change from `int` to `long`, cancellation tokens are mandatory on asynchronous contracts, and persisted events require explicit stable keys and codecs.

Read [CHANGES.md](CHANGES.md) for the complete categorized change inventory and [MIGRATION-2.0.md](MIGRATION-2.0.md) before upgrading an existing application or event store.

## Distribution

The GitHub Release contains seven `.nupkg` files, seven `.snupkg` symbol packages, the change inventory, migration guide, release notes, license, and SHA-256 checksums. No package is published to NuGet.org or another public package registry.

Download the release bundle or individual packages, place the `.nupkg` files in a local directory, and add that directory as a NuGet source:

```bash
dotnet nuget add source /absolute/path/to/packages --name ddd-building-blocks-local
```

Then reference one consistent `2.0.0` package set.

## Known limitation

The legacy RocketLaunch API example still publishes projections asynchronously through its historical in-memory publishing table. Its API integration suite has a known timing race. The new F8 projection runner and checkpoint contracts are deterministic; migration of that legacy example pipeline is outside the 2.0 framework release boundary.

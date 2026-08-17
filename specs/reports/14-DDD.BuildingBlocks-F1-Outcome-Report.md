# DDD.BuildingBlocks – F1 results report and gate G1

As of: August 16, 2026  
Branch: `modernization-2026`  
Goal: technical modernization without domain or framework-semantic changes

## Implemented status

- All projects centrally target `net10.0` with C# 14 and nullable context enabled.
- The SDK is set to the stable .NET SDK `10.0.400` via `global.json`.
- Compiler warnings and .NET analyzers are treated as errors.
- Build metadata, source link, deterministic builds, XML documentation files and symbol packages are centrally configured.
- Missing public XML comments are specifically excluded as existing documentation debts via `CS1591`. This exception does not prevent XML documentation files.
- NuGet versions are managed with Central Package Management in `Directory.Packages.props`; Project files no longer contain individual package versions.
- Only the six projects under `src` are packable. `DDD.BuildingBlocks.Packages.slnf` limits restore, build and pack to these artifacts.
- All six packages contain README, repository metadata, source link-enabled PDBs, and XML documentation; Additionally, `.snupkg` symbol packages are created.
- The missing xUnit adapters of the two domain test projects have been added; the abstract test library is explicitly not a test project.
- The previous nullable warnings have been eliminated without changing domain semantics. The serialized aggregate identifier that was already implemented as nullable is now also correctly described as nullable in the interface and is checked before conversion.
- The no longer needed runtime roll forward workaround has been removed from the dev container.

## Package updates

The stable releases used have been updated within the low-risk framework for F1. This includes in particular the Microsoft Extensions and ASP.NET test packages on `10.0.11`, Testcontainers on `4.14.0`, xUnit on `2.9.3`, Microsoft.NET.Test.Sdk on `18.9.0` and the current stable Azure packages.

Major upgrades with possible behavioral or API changes are intentionally excluded from F1, including Application Insights 3, SqlClient 7, NSubstitute 6, Swashbuckle 10, and xUnit Runner 4. They belong in a separate, independently verifiable modernization stage.

The transitive vulnerability scan does not report any known vulnerable packages for any project.

## Verification

- Normal solution restore: successful.
- Solution build: successful with 0 warnings and 0 errors.
- Core: 57/57 tests successful.
- DevelopmentPackage Integration: 22/22 successful.
- MSSQL: 33 successful, 2 skipped, 0 failed.
- RocketLaunch Domain: 21/21 successful and now discovered.
- LunarOps Domain: 53/53 successful and now discovered.
- RocketLaunch Application: 23/23 successful.
- RocketLaunch ReadModel: 27/27 successful.
- API: 5/7 successful; two errors correspond to the asynchronous projection race accepted in F0 and are not a regression introduced by F1.
- Manual API smoke on .NET 10: Swagger HTTP 200; `GET /crew-members` HTTP 200 with `[]`.
- API compatibility check of all six framework assemblies against the F0 reference commit: no breaking changes found.
- Package build: exactly six `.nupkg` and six `.snupkg`, without warnings.
- Consumer smoke test: new `net10.0` console project was able to restore, compile and run `DDD.BuildingBlocks.Core` from local package feed.

## Gate G1

F1 is technically completed and **ready for review at Gate G1**. No work from F2 was brought forward. Explicit release of G1 is required before changes to framework semantics or domain primitives.

Open, accepted legacy finding: The API integration tests require deterministic synchronization with the asynchronous projection. This should be fixed separately so that the entire suite becomes reliably green.

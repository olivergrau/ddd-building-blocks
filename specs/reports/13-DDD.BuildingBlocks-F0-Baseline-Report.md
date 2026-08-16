# DDD.BuildingBlocks - F0 baseline report

As of: August 16, 2026  
Reference commit: `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`  
Modernization branch: `modernization-2026`

## Result

F0 is completed and documented as a starting point for F1. The environment uses the stable .NET SDK `10.0.400` and the runtime `10.0.11` on Linux x64.

## Reproduced baseline

- Restore: successful, about 16 seconds.
- Build on previous target `net9.0`: successful, 18 compiler warnings.
- Core tests: 57 successful.
- DevelopmentPackage integration tests: 22 successful.
- MSSQL integration tests: 33 passed, 2 skipped; SQL Server was deployed via test containers.
- RocketLaunch Application: 23 successful.
- RocketLaunch ReadModel: 27 successful.
- RocketLaunch and LunarOps domain test assemblies were not discovered because the test adapter was missing from these projects.
- API integration tests: 7 discovered; alternating one or two errors due to the already familiar asynchronous read model/projection race (empty response or 404). This old finding was expressly accepted for entry into F1.
- Manual API smoke test: Swagger and the empty crew member list were accessible.

## Baseline warnings

The 18 warnings consisted of two nullable contract issues in the core, nullable initializations in the examples, and a double `using`. There were no build errors.

## Package baseline

The originally documented call `dotnet pack --no-build` was not reproducible because Debug was previously built but release was expected by default during packaging. The solution also included sample projects. F1 therefore introduces an explicit solution filter file for the six deliverable framework packages.

## Release

The instruction to begin F1 constitutes explicit approval of the F0 gate. The known flaky API test remains documented as a pre-existing issue and is not addressed as a framework semantics change in F1.

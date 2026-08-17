# F10 Outcome Report: Packaging and Stable Release

## Status

F10 is implemented and locally verified. Stable release `v2.0.0` is the approved release target and is marked **READY FOR PLAYGROUND**. GitHub publication is performed by the verified tag workflow.

## Release decisions

- SemVer: stable `2.0.0`, reflecting intentional breaking changes from 1.x.
- License: MIT.
- Distribution: local NuGet folder and GitHub Release assets.
- Registries: no NuGet.org or GitHub Packages publication.
- Signing: not enabled; SHA-256 checksums provide download-integrity verification.
- Future compatible features increment the minor version; compatible fixes increment the patch version.

## Package quality

All seven active source projects inherit consistent package metadata from `Directory.Build.props`: version, MIT license expression, repository and project URLs, release-notes URL, tags, embedded package README, deterministic build, XML documentation, Source Link, and `.snupkg` symbols.

`eng/create-release.sh` builds and packs Release configuration, requires exactly seven `.nupkg` and seven `.snupkg` files, and inspects each NuGet archive for:

- version `2.0.0`;
- MIT license metadata;
- the canonical GitHub repository;
- embedded package README;
- `net10.0` assembly;
- `net10.0` XML documentation.

It then assembles the package set, license, release notes, migration guide, detailed change inventory, compressed bundle, and SHA-256 checksums.

## Consumer verification

`eng/consumer-smoke-test.sh` creates a new .NET 10 console project outside the repository, references all seven packages at exactly `2.0.0`, restores them from the generated local release directory plus NuGet.org for third-party dependencies, compiles, and executes references to Core, In-Memory, PostgreSQL, and SQL Server public types.

This verifies package-to-package dependency resolution rather than accidentally resolving project references from the source tree.

## Release and migration documentation

- `CHANGES.md` is the categorized and extensive 1.x-to-2.x change inventory.
- `MIGRATION-2.0.md` provides the ordered platform, event-contract, repository, dispatch, data, snapshot, projection, validation, cutover, and rollback procedure.
- `RELEASE_NOTES-2.0.0.md` is the concise GitHub Release description.
- The repository and package READMEs link users to release and migration information.

## GitHub release automation

`.github/workflows/release.yml` is triggered by tag `v2.0.0`. It performs a clean Release build, runs framework/example/provider verification including real PostgreSQL and SQL Server Testcontainers, scans direct and transitive packages for known vulnerabilities, creates and inspects the bundle, runs the clean consumer smoke test, retains a workflow artifact, and creates the GitHub Release using the repository-scoped `GITHUB_TOKEN`.

The release attaches all NuGet and symbol packages individually as well as the complete archive, checksums, license, changes, and migration guide. No long-lived credential is stored in the repository.

## Verification

- Release solution build: successful with zero warnings and zero errors.
- Core: 73/73 passed.
- DevelopmentPackage integration: 41/41 passed.
- PostgreSQL integration: 30/30 passed in a real Testcontainer.
- SQL Server integration: 30/30 passed in a real Testcontainer.
- RocketLaunch Application: 23/23; ReadModel: 27/27; Domain: 21/21.
- LunarOps Domain: 53/53.
- Seven `.nupkg` and seven `.snupkg` files created and structurally inspected.
- Clean consumer restore, build, and execution: passed.
- Direct and transitive package vulnerability scan: no known vulnerabilities.
- `git diff --check`: passed.

## Accepted example limitation

The legacy RocketLaunch API example still uses its historical asynchronous in-memory publishing table instead of the F8 projection runner. Its seven-test API suite remains timing-sensitive; the latest Release run passed 6/7 with one projection-read 404. This known example limitation was accepted during earlier gates and does not affect the package/provider release workflow. It remains documented rather than hidden by retrying or weakening assertions.

## Gate G10

The framework package set is release-ready as stable `2.0.0` and **READY FOR PLAYGROUND**. The Git tag and GitHub Release are the immutable publication record.

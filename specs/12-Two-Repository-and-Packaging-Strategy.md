# Persona Simulation Playground

## Two repository and packaging strategy

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Date:** 2026-08-16

## 1. Decision

DDD.BuildingBlocks and Persona Simulation Playground remain two independent Git repositories, solutions, dev containers, build pipelines and release cycles.

The Playground consumes the framework exclusively as versioned NuGet packages. A direct `ProjectReference` to a neighboring repository is not allowed.

## 2. Why this limit makes sense

- Framework remains usable independently;
- public API is actually tested;
- transitive dependencies become visible;
- Playground can pin a known framework version;
- Framework modernization and product development have separate histories;
- a framework error can be traced back to a package status;
- Codex orders remain limited to one repository;
- later publication is possible, but not forced.

The price is an additional pack, publish and restore step. This effort is intentional because it checks the real dependency limit.

## 3. Development flow

```text
Change framework
-> Frameworktests
-> dotnet pack with a prerelease version
-> lokaler NuGet-Feed
-> Playground aktualisiert konkrete Version
-> restore and consumer tests
-> commit the Playground change separately
```

The same package is never released under the same version with different content. Each local iteration receives a new pre-release or unique build metadata, as long as the feed used distinguishes this correctly.

## 4. Local feed

Recommended host path:

```text
~/.local/share/nuget/persona-feed
```

Both dev containers mount it as:

```text
/workspaces/local-nuget-feed
```

Framework publishes there. Playground reads from it. The feed only contains build artifacts and is not included in Git.

## 5. `NuGet.Config`

Conceptual example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-persona" value="/workspaces/local-nuget-feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-persona">
      <package pattern="DDD.BuildingBlocks.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

As soon as a central feed is used, the mapping strategy is expanded accordingly. Package source mapping and feed priority are tested with real restore, not just syntactically configured.

## 6. Versioning flow

### Development statuses

```text
0.8.0-alpha.1
0.8.0-alpha.2
0.8.0-rc.1
```

The specific base version depends on the existing framework history.

### Released status

For example, the Playground pins:

```xml
<PackageVersion Include="DDD.BuildingBlocks.Core" Version="0.8.0-rc.1" />
```

No floating versions. A package update is a visible playground change with restore, build, testing and review.

## 7. Package Compatibility

Run before release:

- Check package contents;
- API compatibility analysis against last released version;
- Consumer smoke test from empty project;
- Examples against packages instead of internal ProjectReferences, at least in a release test;
- Framework Contract Suite;
- Playground Compatibility Suite when available.

## 8. Central feeds

Possible future goals:

| Feed | Suitability |
|---|---|
| GitHub Packages | suitable for GitHub-centered private development |
| private NuGet server | makes sense with complete Homelab sovereignty, additional operation |
| NuGet.org | only for consciously public stable releases |

The local folder feed is sufficient initially. A central feed becomes necessary once CI or a second development computer must restore the same prerelease packages reproducibly.

## 9. CI separation

### Framework CI

```text
restore
build
unit and architecture tests
contract tests
production-provider integration tests
pack
package validation
optional publish after a tag or approval
```

### Playground CI

```text
restore gepinnter Frameworkversion
build
unit and architecture tests
PostgreSQL integration tests
E2E with stub inference
container build later
```

The Playground build does not automatically trigger a framework build from source code.

## 10. Local shared workspace

An optional, not necessarily versioned, multi-root workspace file can display both repositories:

```jsonc
{
  "folders": [
    { "path": "../ddd-building-blocks" },
    { "path": "../persona-simulation-playground" }
  ]
}
```

It's for navigation and comparison, not build. For Codex deployment jobs, a single repository remains the active workspace.

## 11. Upgrade process in the Playground

1. select the desired framework version;
2. Change `Directory.Packages.props`;
3. Do not blanket delete NuGet caches except for diagnostic purposes;
4. Restore with lockfile rules;
5. Architecture, unit and compatibility tests;
6. real provider integration;
7. Check release notes;
8. Commit the change separately.

## 12. Backwards compatibility

Before `1.0`, targeted breaking changes are possible, but require documentation. After stable release, SemVer strictly applies. Persisted event contracts also have their own schema evolution and may not be considered solved by a package major version alone.

## 13. What is expressly avoided

- Git submodules for normal development;
- Copy and paste the framework code into the playground;
- common solution across both repositories as a build basis;
- local ProjectReferences;
- Floating NuGet Versions;
- Overwriting existing package versions;
- Playground-specific special paths in the framework;
- Automatically publish unreviewed packages with every commit.

## 14. Acceptance criteria

- both repositories build independently;
- Framework generates installable packages;
- local feed works in both dev containers;
- Playground only references packages;
- Versions are reproducible and pinned;
- CI remains separated;
- a framework upgrade is visible as an explicit playground change;
- Release and event schema versions are not confused.

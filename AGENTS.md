# DDD.BuildingBlocks contribution rules

## Current modernisation status

- The repository is currently in phase F0 (baseline and reproducibility).
- Do not begin F1 or make framework-semantic changes until the F0 baseline report has been reviewed and the next gate has been explicitly approved.

- Keep the framework independent of Playground product logic.
- Preserve public API and SemVer expectations; document intentional breaking changes.
- Keep provider semantics covered by the provider-contract suite.
- Keep domain primitives free of ASP.NET Core, EF Core, Npgsql, HTTP, and hosting dependencies.
- Run the documented baseline commands before changing framework semantics.
- Stop for review after each modernisation gate; do not combine unrelated stages.

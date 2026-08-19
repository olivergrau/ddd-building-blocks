# DDD.BuildingBlocks 2.1.1

DDD.BuildingBlocks 2.1.1 is a documentation-focused stable patch release. It completes the user-documentation migration to the recommended 2.1 APIs and adds a reproducible MkDocs Material site.

## Highlights

- Complete tutorial coverage for stable aggregate type keys.
- Explicit commit-metadata examples with separate command, correlation, actor, and turn identities.
- Strict hierarchical MkDocs Material documentation.
- Light and dark themes, navigation tabs, sections, search, syntax highlighting, and responsive API tables.
- Isolated pinned documentation dependencies and Make-based local workflows.
- Automated strict documentation build in GitHub Actions.
- Prebuilt static documentation archive in the GitHub Release assets.

## Build and serve the documentation

```bash
make docs-install
make docs
make docs-serve
```

The local site is served at `http://localhost:8000`. The generated static output is written to the ignored `site/` directory.

## Package compatibility

This patch release does not change framework runtime behavior, event-store schemas, provider contracts, or projection and snapshot formats. Consumers can update all DDD.BuildingBlocks package references from `2.1.0` to `2.1.1` directly.

## Distribution

The GitHub Release contains seven `.nupkg` files, seven `.snupkg` symbol packages, a complete release archive, a prebuilt MkDocs site archive, release notes, the full change inventory, migration guidance, the MIT license, and SHA-256 checksums. Packages are not published to NuGet.org.

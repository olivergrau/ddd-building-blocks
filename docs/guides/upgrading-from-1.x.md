# Upgrade from 1.x to 2.0

DDD.BuildingBlocks 2.0 is an intentional SemVer major release. Upgrade the framework, persistence contracts, event serialization, and application dispatch as one coordinated change.

The repository's `CHANGES.md` is the exhaustive categorized inventory; `MIGRATION-2.0.md` is the authoritative release migration checklist. This page explains the user journey.

## Breaking areas

- .NET 10 replaces older target frameworks.
- stream versions change from `int` to `long`;
- `IEventStorageProvider` becomes envelope-based `IEventStoreProvider`;
- persisted CLR type names are replaced by stable event keys and `IEventCodec`;
- the old SQL Server implementation/schema is removed;
- cancellation tokens are mandatory on asynchronous contracts;
- service locator, dependency resolver, and default command processor are removed;
- snapshots use versioned JSON envelopes and a new provider contract;
- projection checkpoint/recovery contracts are new;
- reflection-driven provider uniqueness is removed.

## Recommended sequence

1. Create and verify a recoverable 1.x backup.
2. Move the application to .NET 10 without changing production data.
3. Register stable keys for every historical event type.
4. Add current and historical JSON codec tests/upcasters.
5. Migrate command/event dispatch to Microsoft DI.
6. Replace repository/provider construction and propagate cancellation.
7. Deploy a new PostgreSQL or SQL Server 2.0 schema.
8. Transform/import legacy event history explicitly.
9. Discard and rebuild snapshots.
10. Rebuild projections and compare results.
11. switch all writers together;
12. retain the complete 1.x application/database as the rollback unit until acceptance.

## No mixed mode

Do not combine:

- Core 2.0 with a 1.x provider;
- a 2.0 provider with 1.x serialized rows;
- `int` and `long` version assumptions;
- old file/binary snapshots with the 2.0 codec;
- locator-based and scoped DI dispatch as parallel hidden paths.

## Compatibility proof

Before cutover, prove:

- every historical event key/schema decodes;
- aggregate reconstruction matches the 1.x baseline;
- migrated stream counts and final versions match;
- expected-version concurrency works on migrated streams;
- read models rebuild deterministically;
- backup/restore works with the new provider;
- no 1.x package or schema dependency remains.

## Package acquisition

Download all required `2.0.0` packages from the stable GitHub Release, verify checksums, and use a local or controlled internal NuGet source. Pin exact versions.

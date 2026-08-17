# Database and event-data migration

## Schema migration versus event migration

Provider migrators create and upgrade the DDD.BuildingBlocks 2.0 schema. They do not transform arbitrary legacy event payloads or reinterpret the removed 1.x SQL Server format.

Treat these as separate operations:

- **schema migration:** create provider tables, constraints, indexes, checkpoint store, and snapshot store;
- **event-data migration:** map old type identity/payload/version metadata into valid 2.0 envelopes;
- **read-model migration:** rebuild or transform query models independently.

## Deployment order

1. Back up the current database.
2. Deploy/run the provider migrator with privileged credentials.
3. Validate migration version and expected objects.
4. Run any controlled event import while application writers are stopped or isolated.
5. Validate streams and replay.
6. Start compatible application readers/writers.
7. Rebuild projections.

## Legacy event import

A one-off importer should:

1. read each old stream in order;
2. map old CLR/type identity to a stable registered event key;
3. transform payload into the expected JSON schema;
4. assign zero-based `long` stream versions;
5. preserve event ID, time, correlation, and causation where trustworthy;
6. append each stream atomically or use a purpose-built validated bulk import;
7. compare input/output counts and final versions;
8. instantiate aggregates from the imported stream and compare domain state.

Do not insert invalid envelopes and hope the application can repair them later.

## Aggregate type migration

If an aggregate namespace or CLR name changed, map the stored aggregate type consistently for every stream and event. Repository reads use this discriminator. Test loading through the final deployed aggregate type.

## Position allocation

Global positions must remain unique, monotonic in commit order, and compatible with the provider allocator. After bulk import, ensure the allocator's next value is beyond imported positions. Use provider-specific, transactionally safe procedures.

## Snapshots

Prefer discarding old snapshots and recreating them from validated event streams. Snapshot import adds risk without preserving unique business history.

## Rollback

A safe rollback unit is the complete old application plus its old database. Do not point 1.x code at a 2.0 schema. If 2.0 accepts new writes, returning to 1.x requires a designed reverse transformation or a decision to discard those writes.

## Migration acceptance

Require:

- counts per aggregate type and stream;
- final stream versions;
- event-key/schema distribution;
- aggregate reconstruction samples and edge cases;
- projection rebuild equality;
- concurrency test on migrated streams;
- native backup and restore of the migrated database;
- documented cutover and rollback decision points.

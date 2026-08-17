# Production operations

## Deployment responsibilities

Separate these responsibilities where possible:

- schema migration credentials;
- application event append/read credentials;
- projection read-model/checkpoint credentials;
- backup/restore administration;
- release artifact verification.

Apply least privilege without breaking the transaction boundary required by projection callbacks.

## Observability

Monitor:

- append latency and failure rate;
- concurrency conflict rate by aggregate type;
- stream read latency and decoded event count;
- unknown event/schema and serialization failures;
- latest committed global position;
- checkpoint position and projection lag;
- projection fault status, failed position, and retry count;
- snapshot read/write latency and fallback frequency;
- database connections, locks, transaction duration, storage, and backup freshness.

Correlate command, event, and projection logs with correlation/causation IDs where available.

## Capacity

Measure event rate, average and high-percentile payload size, events per stream, projection write amplification, retention, and replay throughput. Test realistic large batches rather than relying on unit-scale measurements.

Snapshots address long-stream replay cost; they do not reduce event-table retention. Projections may be rebuilt, but event history must be retained according to domain, legal, and recovery requirements.

## Backup policy

Use database-native consistent backups. Include:

- stream and event tables;
- global-position allocator;
- provider migration history;
- projection checkpoints;
- read-model tables when restore time matters;
- snapshots, although they may be discarded.

Store release version, event registry version, and migration procedure alongside backup metadata.

## Restore drills

Regularly restore into an isolated environment and run:

- schema/version validation;
- stream count and final-version checks;
- representative aggregate replay;
- committed-feed scan;
- projection rebuild;
- snapshot fallback test;
- application consumer smoke test.

Measure recovery point and recovery time objectives from evidence.

## Incident rules

- Do not delete or edit event rows during an incident without a reviewed forensic/migration plan.
- Stop a broken projection without stopping unrelated projections.
- Treat unknown event keys and future schema versions as deployment compatibility incidents.
- Treat repeated concurrency conflicts as a domain/workflow signal, not only database noise.
- Preserve failed envelope identity and diagnostics before retrying.
- If event append outcome is uncertain after a network failure, use event IDs and stream state to determine whether it committed before repeating business work.

## Security

Event payloads can contain durable sensitive data. Apply data minimization before persistence, encrypt transport and storage, restrict database access, avoid payload logging, and design legal erasure requirements explicitly. Event sourcing does not exempt a system from privacy obligations.

## Release artifacts

Verify `SHA256SUMS` for downloaded GitHub Release assets. Pin exact package versions and retain the complete package set used by each application deployment.

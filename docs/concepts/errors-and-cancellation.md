# Errors and cancellation

## Error categories

The framework distinguishes domain and infrastructure outcomes through `ErrorClassification`. Important categories include:

- `Validation` and `InputDataError`;
- `DomainRejection` and `InvalidState`;
- `StreamNotFound` and `StreamAlreadyExists`;
- `ConcurrencyConflict`;
- `UnknownEventType` and `UnsupportedSchemaVersion`;
- `SerializationFailure`;
- `TransientProviderFailure` and `PermanentProviderFailure`;
- `Cancellation`;
- `ProgrammingError`.

Use categories to drive boundary behavior, not to hide the concrete exception and diagnostics.

## Expected handling

| Failure | Typical action |
|---|---|
| validation/domain rejection | return a client-visible rejection; do not retry unchanged input |
| concurrency conflict | reload and re-evaluate the command |
| unknown event/schema | stop processing and deploy compatible registry/upcasters |
| transient provider failure | bounded retry with backoff at the operation boundary |
| permanent provider failure | alert and repair configuration/schema/data |
| projection poison event | mark failed, repair, then explicit retry |
| cancellation | stop promptly; do not log as an unexpected failure |

## Cancellation rules

All asynchronous persistence and dispatch contracts require a `CancellationToken`.

- Check cancellation before expensive work.
- Pass the same token into database APIs.
- Never replace a caller token with `CancellationToken.None` for primary work.
- Do not wrap `OperationCanceledException` in provider, serialization, or application exceptions.
- Cleanup or post-commit snapshot work may use an independent token when cancellation must not misreport an already completed event commit.

## Logging

Log stable identifiers, aggregate type, expected/actual version, event key/schema, projection key/position, and correlation ID. Avoid logging full event payloads by default because they may contain sensitive business data.

## Retry ownership

Retry at a boundary that understands idempotency. A repository cannot decide whether repeating a business command is safe after a concurrency conflict. A projection runner can retry the same uncommitted position because its checkpoint transaction defines the idempotency boundary.

# F4 Outcome Report: Async, Cancellation, Error, and DI

## Status

F4 is implemented and verified. Gate G4 is awaiting explicit approval.

## Implemented decisions

- Removed `ServiceLocator`, `IDependencyResolver`, `DefaultCommandProcessor`, `ICommandProcessor`, and the reflection-based `DomainEventNotifier` completely. No compatibility facade remains.
- Introduced `ICommandDispatcher` and `IDomainEventNotifier` implementations backed by `IServiceScopeFactory`; every dispatch or notification owns an independent asynchronous scope.
- Added validated assembly registration. Commands require exactly one handler; missing and duplicate command handlers fail explicitly. Domain events may have multiple subscribers.
- Replaced legacy handler method names with `HandleAsync` and made `CancellationToken` mandatory and last.
- Made cancellation mandatory across repository, event storage, snapshot storage, string storage, aggregate lookup, snapshot creation, and aggregate sourcing boundaries.
- Propagated cancellation into the in-memory, MSSQL, and Azure provider operations. `OperationCanceledException` is never converted into a command result or provider error.
- Added the F4 error categories for validation, domain rejection, stream lifecycle and concurrency, event evolution and serialization, provider failures, and cancellation.
- Migrated RocketLaunch startup, handlers, API endpoints, projectors, and tests to the new contracts.

## Verification

- Solution restore: successful.
- Solution build: successful with zero warnings and zero errors.
- Package build: successful for all six packages, including symbol packages.
- New dispatch contract tests cover missing handlers, duplicate handlers, cancellation preservation, scoped lifetime, and parallel dispatch without shared scope state.
- Existing framework, example, and provider tests were migrated to the mandatory cancellation contracts.
- Latest results: Core 64/64, DevelopmentPackage 22/22, MSSQL 33 passed and 2 skipped, RocketLaunch Application 23/23, ReadModel 27/27, Domain 21/21, and LunarOps Domain 53/53.
- RocketLaunch API passed 6/7; the remaining 404 is the previously accepted asynchronous projection timing race.

The pre-existing RocketLaunch projection timing flakiness remains unrelated to F4.

## Breaking changes

F4 intentionally removes obsolete public APIs and changes asynchronous method signatures. Consumers must register dispatch through `AddDddBuildingBlocksDispatching`, inject `ICommandDispatcher` or `IDomainEventNotifier`, implement `HandleAsync`, and pass cancellation tokens explicitly.

## Gate G4 review

Review the removed compatibility surface, required cancellation signatures, error taxonomy, and scoped dispatcher semantics. Do not begin F5 before explicit Gate G4 approval.

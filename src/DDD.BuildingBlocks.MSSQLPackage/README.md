# MSSQL Server based implementations for DDD.BuildingBlocks.Core

This package is divided into two sections:
1. Provider implementations
2. Service implementations

## Providers
1. EventStorageProvider
2. SnapshotStorageProvider

### Remarks
EventStorageProvider implements the interface 'IEventStorageProvider' of the BuildingBlocks Core package and the
matching SnapshotStorageProvider based on the interface 'ISnapshotStorageProvider'.

*Notes*:

- Make sure that you always use both implementations (EventStorage/SnapshotStorage) from one package only. <br/>
So either use only the EventStorageProvider implementation without snapshots or with the supplied SnapshotImplementation from the same package.

## Services
1. AggregateInformationService
2. EventProcessingServiceBackgroundWorker

### Remarks

*Notes*:

- The same applies here as for the providers, only use both implementations together from one package.

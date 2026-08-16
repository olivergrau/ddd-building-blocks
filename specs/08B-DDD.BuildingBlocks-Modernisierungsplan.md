# Persona Simulation Playground

## DDD.BuildingBlocks Modernisierungsplan

**Status:** PROPOSED  
**Version:** 1.1  
**Datum:** 2026-08-16  
**Basis:** Frameworkanalyse auf Commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Ziel

Das Framework wird vor dem produktiven Einsatz auf einen belastbaren .NET-10-Stand gebracht. Die Modernisierung bleibt ein eigenes, kleinschrittiges Vorprojekt. Sie soll weder den Playground vorwegnehmen noch eine zweite Event-Sourcing-Infrastruktur danebenstellen.

## 2. Verbindliche Grenzen

### In Scope

- .NET 10 LTS und aktuelle kompatible Packages;
- Warnungsfreiheit und Nullable-Härtung;
- Cancellation in I/O-Contracts;
- saubere Aggregate-Typtrennung;
- Event Envelope und stabile Eventtyp-Registry;
- Optimistic-Concurrency-Contract;
- produktiver Event-Store-Adapter und Snapshotprovider nach einem expliziten Technologieentscheid;
- belastbare Projection Checkpoints und Rebuild;
- In-Memory-Testprovider mit produktionsnaher Semantik;
- explizite DI-Registrierung;
- Tests, Migrationen und Dokumentation.

### Out of Scope

- allgemeine Message-Broker-Unterstützung;
- vorsorgliche Unterstützung mehrerer produktiver Event-Store-Technologien gleichzeitig;
- Multi-Tenant-Unterstützung;
- verteilte Sagas;
- Actor Runtime;
- Azure-Modernisierung ohne Playground-Bedarf;
- generisches Plugin-System;
- automatische Eventmigration aller denkbaren Altversionen.

## 3. Zielarchitektur des Frameworkkerns

```text
DDD.BuildingBlocks.Core
  Domain primitives
  Event-sourced aggregate primitives
  Event envelope contracts
  Command/result contracts
  Repository/provider contracts

DDD.BuildingBlocks.<ChosenProvider>
  Event store adapter
  Snapshot store adapter
  Projection feed/checkpoints
  provider-specific migrations or configuration

DDD.BuildingBlocks.Testing
  In-memory providers
  Given-When-Then harness

DDD.BuildingBlocks.DependencyInjection
  Explicit registrations
```

## 4. Etappen

### M1: Baseline reproduzieren

**Ziel:** Aktuellen Stand unverändert bauen und testen.

**Akzeptanz:**

- SDK-Version gepinnt;
- Restore, Build und Tests reproduzierbar;
- Testkategorien dokumentiert;
- bekannte fehlschlagende Tests sind erklärt, nicht ignoriert.

**Stop-Punkt:** Keine fachlichen Änderungen.

### M2: .NET 10 und Compilerhärtung

**Ziel:** Technische Aktualisierung ohne Semantikänderung.

**Akzeptanz:**

- alle relevanten Projekte auf `net10.0`;
- zentrale Paketversionen konsistent;
- Nullable aktiviert;
- Warnings as Errors für eigenen Code;
- Linux-Build erfolgreich.

### M3: Aggregate-Typen und Kerncontracts

**Ziel:** Selektives Event Sourcing im Typmodell ausdrücken.

**Akzeptanz:**

- nicht eventgesourcter Aggregate Root vorhanden;
- bestehender ES-Typ bleibt zunächst kompatibel;
- uncommitted Events nur lesbar exponiert;
- Replay und neues Apply eindeutig getrennt;
- Tests für Versionsübergänge und fehlende Apply-Handler.

### M4: Event Envelope und Event Codec

**Ziel:** Persistierte Verträge von CLR-Typnamen entkoppeln.

Minimaler Envelope:

```text
EventId
StreamId
AggregateType
StreamVersion
GlobalPosition
EventType
SchemaVersion
OccurredAt
CommittedAt
CorrelationId
CausationId
CommandId optional
TurnId optional
Actor optional
Payload
```

**Akzeptanz:**

- stabile logische Eventnamen;
- explizite Registry;
- unbekannte Eventtypen führen zu klassifiziertem Fehler;
- mindestens ein Upcaster-Test;
- keine Assembly Qualified Names in neuen Datensätzen.

### M5: Async und Cancellation

**Ziel:** Kontrollierbare I/O-Operationen.

**Akzeptanz:**

- `CancellationToken` in Repository, Provider, Snapshot, Dispatcher und Handler;
- Cancellation wird nicht als Domainfehler klassifiziert;
- keine Cancellation in reinen Domain-Apply-Methoden;
- Tests für Abbruch vor und während I/O.

### M6: Persistence Decision Gate und produktiver Event Store

**Ziel:** Event-Store-Technologie anhand der verbindlichen Fähigkeiten und des Betriebsaufwands auswählen und anschließend genau einen produktiven Adapter umsetzen.

Bewertete Optionen:

```text
DDD.BuildingBlocks PostgreSQL Provider
Adaption der vorhandenen relationalen Providerlogik
externer Event Store mit DDD.BuildingBlocks Adapter
```

PostgreSQL bleibt die bevorzugte Ausgangshypothese, solange eine externe Lösung keinen nachweisbaren Zusatznutzen bietet.

Bei einer relationalen Implementierung werden mindestens folgende Speicherklassen benötigt:

```text
event_streams
events
snapshots
projection_checkpoints
```

**Akzeptanz:**

- eindeutiger Stream über Aggregate-Typ und Aggregate-ID;
- atomarer Expected-Version-Check;
- append mehrerer Events in einer Transaktion;
- monotone globale Position;
- Event-ID eindeutig;
- Streamlesen ab Version und Feedlesen ab Position;
- reproduzierbare Migrationen oder gleichwertige Store-Konfiguration;
- Testcontainers-Tests für Create, Append, Conflict, Replay und Parallelität.

### M7: Projection Pipeline

**Ziel:** Persist-before-publish, Recovery und Rebuild.

**Akzeptanz:**

- committed Event Feed aus dem gewählten produktiven Store;
- Checkpoint pro Projection und Projection-Version;
- idempotente erneute Zustellung;
- Projection-Update und Checkpoint in einer Transaktion;
- Fehler stoppt nur die betroffene Projection;
- Rebuild ab Position null;
- kein globaler positionsbasierter Zeilenoffset.

### M8: Snapshots

**Ziel:** Optionale, verwerfbare Rehydrationsoptimierung.

**Akzeptanz:**

- Snapshot ist einer Streamversion zugeordnet;
- inkompatibler Snapshot kann verworfen werden;
- Rehydration ohne Snapshot bleibt vollständig möglich;
- Snapshotfehler ändert keinen erfolgreichen Eventcommit;
- Äquivalenztest Event-Replay gegen Snapshot plus Restevents.

### M9: DI und Dispatch

**Ziel:** Explizite, überprüfbare Handlerauflösung.

**Akzeptanz:**

- kein Service Locator im normalen Laufzeitpfad;
- Command- und Projection-Handler explizit registriert;
- doppelte Handlerregistrierung wird beim Start erkannt;
- scoped Dependencies funktionieren;
- Reflection Discovery ist optional und validiert.

### M10: Release Gate

**Ziel:** Freigabe für den ersten Playground-Slice.

**Akzeptanz:**

- alle Unit- und Integrationstests grün;
- API-Dokumentation aktualisiert;
- Migrations- und Recovery-Test erfolgreich;
- kleines Playground-nahes Beispiel für eventgesourcte `Session` und klassisches `Persona` Aggregate;
- Frameworkversion gepinnt;
- keine offene kritische Finding-Kategorie.

## 5. Priorisierung

| Priorität | Punkte |
|---|---|
| Blockierend vor Playground-Implementierung | M1 bis M5 sowie M9 für den benötigten Kernumfang |
| Blockierend vor produktiver Session-Persistenz | M6 |
| Blockierend vor produktiven Read Models | M7 |
| Vor Aktivierung von Snapshots | M8 |
| Vor breiter Application-Integration | M9 |
| Vor erstem produktivem Slice | M10 |

## 6. Bewusst vertagte Fragen

- Ob die existierende Klasse `AggregateRoot<TKey>` in einer Major Version umbenannt wird.
- Ob der Event Codec langfristig `System.Text.Json` oder ein austauschbares Codec-Interface verwendet. Für den Playground genügt ein explizit versionierter Codec.
- Ob Event Store und Read Models dieselbe PostgreSQL-Instanz verwenden oder der Event Store später extern betrieben wird. Für den MVP bleibt eine gemeinsame PostgreSQL-Instanz die einfachere Ausgangshypothese.
- Ab welcher Streamlänge Snapshots aktiviert werden. Dies wird gemessen, nicht geraten.

## 7. Entscheidungsregel bei Konflikten

Wenn Framework-Kompatibilität und Playground-Invariants kollidieren, gilt:

1. Domain- und Persistenzinvariants des Playground bleiben erhalten.
2. Das Framework wird angepasst.
3. Eine parallele zweite ES-Infrastruktur ist nicht zulässig.
4. Brechende Frameworkänderungen werden nur vorgenommen, wenn eine kompatible Ergänzung die Semantik verschleiern würde.

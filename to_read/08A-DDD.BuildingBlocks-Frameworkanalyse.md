# Persona Simulation Playground

## DDD.BuildingBlocks Frameworkanalyse

**Status:** REVIEWED, technische Ausführung noch zu verifizieren  
**Version:** 1.0  
**Datum:** 2026-08-16  
**Repository:** `olivergrau/ddd-building-blocks`  
**Referenzcommit:** `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Ergebnis in einem Satz

`DDD.BuildingBlocks` ist eine brauchbare und fachlich passende Ausgangsbasis, soll aber nicht unverändert in den Playground übernommen werden. Empfohlen wird **Option B: gezielte Modernisierung**, wobei Aggregate-, Value-Object-, Event-Sourcing- und Snapshot-Grundideen erhalten bleiben und Persistenz, Eventverträge, Projektionen, Cancellation und DI gezielt gehärtet werden.

Es wurde kein Widerspruch gefunden, der den geplanten Playground grundsätzlich blockiert.

## 2. Bewertungsmaßstab

Die Analyse prüft das Framework gegen die bereits verbindlichen Entscheidungen:

- taktische Typen aus `DDD.BuildingBlocks` werden im Domain Model verwendet;
- Event Sourcing gilt für alle echten fachlichen Aggregate;
- technische Records und Read Models bleiben konventionell persistiert, sind aber keine Aggregate;
- CQRS und rekonstruierbare Read Models sind verbindlich;
- PostgreSQL ist produktiver Provider;
- Persistieren geschieht vor Publizieren;
- die Anwendung bleibt ein modularer Monolith in einem .NET-Prozess;
- Domain-Apply muss deterministisch und frei von Seiteneffekten sein;
- Projection Recovery und vollständiger Rebuild müssen möglich sein;
- stale Inference Results müssen über Optimistic Concurrency abgewehrt werden.

## 3. Repository- und Paketübersicht

| Paket | Zweck | Bewertung für Playground |
|---|---|---|
| `DDD.BuildingBlocks.Core` | Aggregate, Entities, Value Objects, Commands, Events, Event-Sourcing-Repository, Snapshots | Basis verwenden und modernisieren |
| `DDD.BuildingBlocks.DevelopmentPackage` | In-Memory-Provider, dateibasierter Provider, In-Memory-Publikation | Nur für Tests und lokale Entwicklung |
| `DDD.BuildingBlocks.MSSQLPackage` | SQL-Server-Eventstore, Snapshots, Eventverarbeitung | Nicht produktiv übernehmen, aber als Referenz nutzen |
| `DDD.BuildingBlocks.AzurePackage` | Azure Service Bus und Blob Storage | Nicht für den MVP benötigt |
| `DDD.BuildingBlocks.DI.Extensions` | DI-Hilfen und Service Locator | Stark reduzieren beziehungsweise ersetzen |
| `DDD.BuildingBlocks.Hosting.Background` | Hosted-Service-Hilfen | Nur nach Modernisierung selektiv verwenden |

Die Examples `RocketLaunch` und `LunarOps` zeigen Aggregate, Commands, Domain Services, Snapshots, Read-Model-Projektoren und Tests. Sie sind als Nutzungsbeispiele wertvoll, jedoch nicht in allen Punkten normative Architekturvorlagen.

## 4. Positive Befunde

### 4.1 Taktische DDD-Typen

Vorhanden sind:

- `Entity<TKey>`;
- `EntityId<TKey>`;
- `ValueObject<T>`;
- `AggregateRoot<TKey>`;
- `DomainRelation`;
- domänenspezifische Fehlerklassen.

Das passt grundsätzlich zu der gewünschten expliziten Modellierung mit IDs, Entities und Value Objects.

### 4.2 Event-Sourcing-Grundmechanik

Das Framework unterstützt:

- uncommitted Events im Aggregate;
- unmittelbares Apply bei neuen Events;
- Replay historischer Events;
- Aggregate-Versionen;
- erwartete Version beim Append;
- Event-Sourcing-Repository;
- Provider-Abstraktion;
- optionale Snapshots;
- In-Memory-Provider für Tests.

Die grundlegende Semantik ist mit dem geplanten `Session` Aggregate vereinbar.

### 4.3 Optimistic Concurrency

Das Framework besitzt sowohl eine Vorprüfung im Repository als auch eine atomare Versionsprüfung im MSSQL-Provider. Der wichtige Teil ist die Prüfung innerhalb der Speichertransaktion. Dieses Prinzip kann im PostgreSQL-Provider erhalten und verbessert werden.

Damit lässt sich der geplante Schutz gegen stale Inference Results abbilden:

```text
Turn basiert auf Session-Version 41
Human pausiert Session, neue Version 42
Turn versucht Append mit ExpectedVersion 41
Append wird abgelehnt
Inference Result wird verworfen
```

### 4.4 Snapshots

Snapshots sind optional und werden getrennt vom Event Stream gespeichert. Das entspricht der Entscheidung, dass Snapshots nur Performanceoptimierungen sind.

### 4.5 Examples und Tests

Das Repository enthält:

- Unit Tests für Aggregate, Entities, Value Objects, Commands und Eventbenachrichtigung;
- Integrationstests für In-Memory- und MSSQL-Persistenz;
- Parallelitäts- und Stresstests für MSSQL;
- Snapshot-Tests;
- Projector-Tests in den Examples;
- API- und Application-Beispiele.

Das ist eine gute Ausgangslage für eine kontrollierte Modernisierung.

## 5. Capability Matrix

| Anforderung | Stand | Bewertung | Maßnahme |
|---|---|---|---|
| Taktische DDD-Typen | vorhanden | geeignet | API behutsam modernisieren |
| Event-sourced Aggregate | vorhanden | geeignet mit Änderungen | Metadaten, Typidentität, Versionierung härten |
| Zustandsbasiertes Aggregate | kein eigener Aggregate-Root-Typ | kein Defizit im fokussierten Framework | bewusst nicht ergänzen |
| Einheitliches Event-Sourcing-Write-Model | vorhanden | geeignet mit Härtung | Root eindeutig benennen und dokumentieren |
| Optimistic Concurrency | vorhanden | brauchbar | atomare Provider-Prüfung verbindlich machen |
| Uncommitted Events | vorhanden | geeignet | immutable Exposition sicherstellen |
| Event Metadata | teilweise | unzureichend | Envelope ergänzen |
| Event Schema Version | `ClassVersion` vorhanden | teilweise | stabile Eventnamen und Upcaster-Konzept ergänzen |
| Eventtyp-Auflösung | CLR-/Namensreflexion | riskant | stabile logische Type Keys verwenden |
| Cancellation | weitgehend fehlend | unzureichend | `CancellationToken` durchgängig ergänzen |
| Async | vorhanden | teilweise | Contracts und Fehlerbehandlung modernisieren |
| PostgreSQL | fehlt | Lücke | neuen Provider implementieren |
| In-Memory Tests | vorhanden | geeignet mit Härtung | Thread Safety und Concurrency angleichen |
| Snapshots | vorhanden | brauchbar | atomare Semantik und Kompatibilität härten |
| Persist-before-publish | im MSSQL-Pfad grundsätzlich gegeben | teilweise | über durable event position und Dispatcher garantieren |
| Projection Checkpoints | globaler Offset vorhanden | unzureichend | per Projection, transaktional und idempotent neu gestalten |
| Projection Rebuild | nicht ausreichend formalisiert | Lücke | Reset, Replay und Versionierung ergänzen |
| DI | Reflection plus Service Locator | riskant | explizite Registrierung verwenden |
| Nullable | aktiviert, aber Warnungen sichtbar | teilweise | Warnungsfreiheit als Gate |
| Linux/Container | Core plattformneutral | geeignet | PostgreSQL-Tests in Linux-Containern |
| .NET-Version | `net9.0` | kurzfristig unterstützt | auf .NET 10 LTS aktualisieren |

## 6. Wesentliche technische Risiken und Lösungen

### F-01: `AggregateRoot<TKey>` ist semantisch ein Event-Sourcing-Root

Der vorhandene `AggregateRoot<TKey>` implementiert zwingend `IEventSourcingBasedAggregate`. Das Verhalten passt zur nun bestätigten Frameworkausrichtung, der Name drückt die Semantik jedoch nicht deutlich genug aus.

**Auswirkung:** `Persona`, `Scenario`, `PersonaRelationship` und `Session` können dasselbe Write-Modell verwenden. Technische Records benötigen keine Aggregate-Basisklasse.

**Lösungsvorschlag:** Die bestehende Abstraktion in einer Major Version eindeutig benennen:

```text
Entity<TKey>
  └── EventSourcedAggregateRoot<TKey>
```

Ein `ConventionalAggregateRoot<TKey>` und ein allgemeines `IAggregateRoot<TKey>` werden nicht vorsorglich eingeführt. Falls Kompatibilität zu bestehenden Verbrauchern nötig ist, kann der alte Name für eine klar begrenzte Übergangsphase als obsoleter Alias bestehen.

### F-02: Persistierte Eventtypen hängen an CLR-Namen

Events werden über einfachen Klassennamen und teilweise `AssemblyQualifiedName` identifiziert. Einfache Namen können kollidieren. Assemblynamen und Versionen sind keine stabilen fachlichen Verträge.

**Lösungsvorschlag:** Persistierter `EventType` wird ein expliziter stabiler String, beispielsweise `session.participant-spoke`. Die Zuordnung zu CLR-Typen erfolgt über ein explizites Registry. `SchemaVersion` wird separat gespeichert. Upcaster lesen ältere Schemata.

### F-03: Eventmetadaten sind unvollständig

Vorhanden sind Aggregate-ID, Zielversion, Commit-Zeit, Correlation-ID und Class-Version. Es fehlen insbesondere:

- `EventId`;
- `CausationId`;
- `CommandId`;
- globale Eventposition;
- sauberer Actor-Verweis;
- optional `TurnId` und technische Inference-Referenz.

**Lösungsvorschlag:** Fachliches Event und persistierter `EventEnvelope` werden getrennt. Das Event enthält nur fachliche Payload. Der Envelope trägt technische Metadaten.

### F-04: Projection Offset ist nicht robust genug

Der MSSQL-Worker verwendet einen numerischen Zeilenoffset über eine nach Zeit und Streamversion sortierte Abfrage. Das ist für belastbares Recovery ungeeignet:

- Zeitstempel und Streamversion bilden keine garantiert eindeutige globale Reihenfolge;
- OFFSET ist positionsbezogen und kann bei veränderter Datenmenge problematisch sein;
- Fortschritt wird pro Worker, nicht sauber pro Projection Contract geführt;
- Idempotenz ist nicht durch einen Event-Identifier abgesichert;
- Rebuild und Parallelbetrieb sind nicht ausreichend formalisiert.

**Lösungsvorschlag:** PostgreSQL-Eventstore erhält eine monotone globale `position BIGINT`. Jede Projection führt einen Checkpoint über `(projection_name, projection_version, last_position)`. Eventanwendung und Checkpoint-Update erfolgen in derselben Read-Store-Transaktion. Doppelte Zustellung wird über `EventId` oder Position sicher erkannt.

### F-05: Cancellation fehlt in den Kerncontracts

Repository-, Storage-, Command- und Eventhandler-Methoden akzeptieren überwiegend keinen `CancellationToken`.

**Lösungsvorschlag:** Cancellation wird in allen I/O- und Orchestrierungsgrenzen ergänzt. Domainmethoden selbst benötigen keinen CancellationToken, solange sie rein synchron und deterministisch bleiben.

### F-06: Reflection und Service Locator

Command- und Eventhandler werden teilweise über Reflection, `Activator` und einen optionalen Service Locator gefunden beziehungsweise erzeugt.

**Lösungsvorschlag:** Explizite DI-Registrierung und typisierte Dispatcher verwenden. Reflection kann höchstens beim Start zur validierten Registrierung eingesetzt werden, nicht als verdeckte Laufzeitabhängigkeit.

### F-07: In-Memory-Provider entspricht nicht vollständig dem Produktionsverhalten

Die internen Collections sind nicht umfassend threadsicher. Concurrency und Publication unterscheiden sich vom MSSQL-Pfad.

**Lösungsvorschlag:** Der In-Memory-Provider wird deterministisch und thread-safe gemacht und muss dieselben Expected-Version-Regeln wie PostgreSQL erzwingen. Er bleibt Testprovider und ist keine produktive Persistenz.

### F-08: Eventpublikation und Snapshotfehler

Beim In-Memory-Pfad können Events bereits in eine Queue gelangen, bevor der Repository-Ablauf vollständig abgeschlossen ist. Snapshots werden nach Eventcommit gespeichert. Ein Snapshotfehler darf keinen erfolgreichen Eventcommit semantisch zurücknehmen.

**Lösungsvorschlag:** Eventcommit ist der einzige fachliche Commit. Snapshotfehler werden separat behandelt. Produktive Projektionen lesen ausschließlich committed Events aus dem Eventstore. In-Memory-Publikation wird an dieselbe Semantik angepasst.

### F-09: Versionsmodell und Zeittypen

Das Framework verwendet `int` und eine nullbasierte interne Eventversion. Das ist technisch funktionsfähig, aber in Verträgen missverständlich. Zeitpunkte sind `DateTime`.

**Lösungsvorschlag:**

- Streamversion intern eindeutig dokumentieren: `NoStream = -1`, erstes Event erzeugt Version `0`;
- für neue Persistenzspalten `BIGINT` und C# `long` vorsehen;
- `DateTimeOffset` beziehungsweise UTC `Instant`-Semantik verwenden;
- UI-Sequenzen nicht mit Aggregate-Versionen verwechseln.

### F-10: .NET-Zielversion

Das Repository zielt auf .NET 9. Zum Analysezeitpunkt ist .NET 10 die aktuelle LTS-Version und bis November 2028 unterstützt. .NET 9 ist STS und nur bis November 2026 unterstützt.

**Lösungsvorschlag:** Framework und Playground auf .NET 10 LTS ausrichten. Quelle: Microsofts [.NET Releases and Support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support).

## 7. Abhängigkeitsrichtung

Die direkte Domain-Abhängigkeit auf `DDD.BuildingBlocks.Core` ist laut Architecture Decisions ausdrücklich erlaubt. Sie bleibt vertretbar, wenn Core nur taktische Domain- und ES-Grundtypen enthält.

Nicht akzeptabel wären transitive Abhängigkeiten des Domain-Projekts auf:

- ASP.NET Core;
- EF Core oder Npgsql;
- HTTP;
- vLLM/OpenAI DTOs;
- konkrete Repository-Provider;
- Background Hosting.

Die heutige Core-Abhängigkeit auf Logging und Newtonsoft.Json sollte reduziert werden. Serialisierung gehört in Provider beziehungsweise Event-Codec-Komponenten, nicht in die Domain-Basistypen.

## 8. Build- und Testnachweis

Die statische Codeanalyse wurde auf dem genannten Commit durchgeführt. In der verfügbaren Arbeitsumgebung war kein `dotnet` SDK installiert, daher konnten Build und Tests hier nicht ausgeführt werden.

Das ist kein Modellierungsblocker, aber ein verbindliches Gate vor der ersten Frameworkänderung:

```text
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Zusätzlich sind PostgreSQL-Integrationstests per Testcontainers erforderlich.

## 9. Verwendungsentscheidung

### Entscheidung: gezielt modernisieren

Verwendet werden sollen:

- Entity-, ID- und Value-Object-Grundideen;
- Event-sourced Aggregate-Mechanik;
- uncommitted Events und Replay;
- Provider-Grenze;
- Optimistic Concurrency als Prinzip;
- optionale Snapshots;
- Given-When-Then-Teststil.

Gezielt ersetzt oder erweitert werden sollen:

- Event Envelope und Metadaten;
- stabile Eventtyp-Registry;
- PostgreSQL-Provider;
- Projection Dispatch, Checkpoints und Rebuild;
- Cancellation;
- DI-Integration;
- eindeutig benannter und gehärteter Event-Sourcing-Root;
- Serialisierung;
- Zeit- und Versionssemantik.

Nicht übernommen werden sollen:

- Azure-Pakete für den MVP;
- MSSQL als Produktprovider;
- Service-Locator-basierte Laufzeitauflösung;
- positionsbasierter Projection Offset;
- Assembly Qualified Names als persistente Eventverträge.

## 10. Konsequenz für Arbeitspaket 3

Die Domainverträge werden nicht an problematische Details des aktuellen Frameworks angepasst. Stattdessen gelten folgende Leitlinien:

- alle echten Playground-Domain-Aggregate werden eventgesourct modelliert;
- konventionelle technische Persistenz bleibt außerhalb der Aggregate-Hierarchie;
- Events besitzen fachlich kleine Payloads;
- Metadaten liegen im Envelope;
- Commands tragen `CommandId` und bei Änderungen eine Expected Version;
- Apply bleibt synchron, deterministisch und seiteneffektfrei;
- C#-Skizzen in Phase 3B zeigen Zielsemantik und sind keine exakte Kopie der heutigen Framework-API.

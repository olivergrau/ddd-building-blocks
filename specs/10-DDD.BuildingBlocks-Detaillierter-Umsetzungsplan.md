# DDD.BuildingBlocks

## Detaillierter Umsetzungsplan zur Modernisierung und Playground-Freigabe

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16  
**Ausgangsstand:** Commit `26faa4b7226f070a30ae7bb8e1a4cf79b0bba5ad`

## 1. Ziel

DDD.BuildingBlocks wird in einem eigenständigen Repository auf einen belastbaren .NET-10-Stand gebracht, als versionierte NuGet-Pakete bereitgestellt und erst danach vom Persona Simulation Playground verwendet.

Die Modernisierung erhält die tragfähigen fachlichen Grundideen des Frameworks. Sie ist keine Neuentwicklung unter gleichem Namen. Problematische technische Kopplungen werden jedoch nicht aus Kompatibilitätsgründen in den Playground weitergetragen.

## 2. Verbindliche Ergebnisstruktur

Die endgültigen Paketnamen werden nach Bestandsaufnahme bestätigt. Fachlich wird folgende Trennung angestrebt:

```text
DDD.BuildingBlocks.Core
  taktische Domain-Primitiven
  klassische Aggregate Roots
  eventgesourcte Aggregate Roots
  Domain Events und fachneutrale Contracts

DDD.BuildingBlocks.EventSourcing
  Envelope, Registry, Codec
  Repository- und Store-Contracts
  Snapshots

DDD.BuildingBlocks.Projections
  committed Event Feed
  Projection Contracts
  Checkpoints, Retry und Rebuild

DDD.BuildingBlocks.DependencyInjection
  explizite Registrierungen und Startup Validation

DDD.BuildingBlocks.Testing
  Given-When-Then Harness
  In-Memory-Provider
  Provider Contract Suites

DDD.BuildingBlocks.<ProductProvider>
  genau ein produktiver Event-Store-Adapter
  Snapshot- und Feed-Integration
```

Diese Struktur ist Zielrichtung, keine Vorentscheidung für eine sofortige Paketaufspaltung. Wenn bestehende Packages sauber modernisiert werden können, ist eine kompatible Evolution günstiger als eine künstliche Neuordnung.

## 3. Arbeitsregeln

1. Jede Etappe besitzt einen eigenen Branch oder eine klar abgegrenzte Commitfolge.
2. Vor semantischen Änderungen wird die unveränderte Baseline festgehalten.
3. Öffentliche API-Änderungen werden mit Compatibility Report und Migrationshinweis geprüft.
4. Kein Playground-Code wird in das Framework kopiert.
5. Playground-nahe Beispiele dürfen fachlich abstrahiert sein, aber keine Produktlogik enthalten.
6. Tests werden mit der jeweiligen Änderung ergänzt.
7. Paketierung wird früh geprüft, nicht erst am Ende.
8. Ein grüner Build ersetzt keine Architekturabnahme.
9. Ungeplante Refactorings werden als Folgeetappe notiert.
10. Nach jedem Gate stoppt Codex zur Sichtung.

## 4. Phase F0: Baseline und Reproduzierbarkeit

### F0.1 Referenzstand sichern

- Referenzcommit bestätigen;
- Branchschutz und Arbeitsbranch festlegen;
- Tags und bestehende Paketversionen inventarisieren;
- öffentliche Packages und Abhängigkeiten auflisten;
- Examples und Testprojekte kategorisieren;
- bestehende CI-Konfiguration sichern.

### F0.2 Buildumgebung festlegen

- `global.json` auf gewünschtes .NET-10-SDK pinnen;
- Devcontainer zunächst ohne Codeänderung aufbauen;
- Restore, Build, Test und Pack als reproduzierbare Befehle dokumentieren;
- Linux als verbindliche Entwicklungs- und CI-Plattform setzen;
- Architekturabhängigkeiten für `linux-x64` und gegebenenfalls `linux-arm64` prüfen.

### F0.3 Baseline ausführen

```text
dotnet --info
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack --no-build --configuration Debug
```

Zusätzlich werden Testanzahl, Dauer, Warnungen, ausgeschlossene Tests und externe Voraussetzungen erfasst.

### F0.4 Ergebnis

Ein Baseline-Bericht klassifiziert jeden Befund als:

```text
bestehender Defekt
Modernisierungsblocker
akzeptierte technische Schuld
veraltetes Beispiel
fehlender Testnachweis
```

**Gate G0:** Keine Änderung vor gemeinsamer Sichtung des Baseline-Berichts.

## 5. Phase F1: Buildsystem und .NET 10

### F1.1 Zentrale Buildkonfiguration

- `Directory.Build.props` für Sprache, Nullable und Analyzers;
- `Directory.Packages.props` für Central Package Management;
- gemeinsame Versions- und Repositorymetadaten;
- deterministische Builds;
- SourceLink und Repository-Informationen für Packages;
- Symbolpakete und XML-Dokumentation für öffentliche APIs.

### F1.2 Paketaktualisierung

Jedes Update wird klassifiziert:

- sicher und mechanisch;
- API-anpassend;
- semantisch riskant;
- nicht mehr benötigt;
- nur für veraltete Provider relevant.

Große Paketupdates werden nicht in einem undifferenzierten Commit gebündelt.

### F1.3 Compilerhärtung

- Nullable für eigenen Code aktivieren;
- Warnungen stufenweise bereinigen;
- `TreatWarningsAsErrors` für CI und eigenen Code;
- generierten Code und unvermeidbare Fremdwarnungen gezielt behandeln;
- keine breite `NoWarn`-Liste als Abkürzung.

### F1.4 Akzeptanz

- Build unter Linux grün;
- bestehende Semantik durch Regressionstests bestätigt;
- Paketartefakte werden erzeugt;
- keine unerklärten Warnungen.

**Gate G1:** Technische Migration getrennt von fachlichen Contractänderungen abnehmen.

## 6. Phase F2: Taktische Domain-Primitiven

### F2.1 Öffentliche API inventarisieren

Für `Entity`, `EntityId`, `ValueObject`, Aggregate Roots, Domain Errors und Eventbasistypen werden dokumentiert:

- öffentliche und geschützte Member;
- Equality-Semantik;
- Mutability;
- Serialisierungsannahmen;
- Reflection-Annahmen;
- bekannte Verbraucher in Examples und Tests.

### F2.2 Klassische Aggregate Root

Eine fachneutrale Root-Abstraktion wird ergänzt, die keine Event-Sourcing-Fähigkeit erzwingt. Sie muss für `Persona`, `Scenario` und globale Relationships geeignet sein.

Zu entscheiden sind:

- Name und Kompatibilitätsstrategie;
- Revisions- beziehungsweise Concurrency-Semantik;
- Schutz der Identität;
- Domain-Event-Unterstützung ohne Event Sourcing, falls tatsächlich benötigt.

### F2.3 Eventgesourcte Aggregate Root

- Replay und Erzeugung neuer Events klar trennen;
- uncommitted Events unveränderlich exponieren;
- Apply-Handler vollständig validieren;
- `NoStream` und erste Version eindeutig definieren;
- Versionsbereich auf `long` vorbereiten;
- Clear-Uncommitted erst nach erfolgreichem Commit;
- keine Infrastruktur- oder Serializerabhängigkeit.

### F2.4 Tests

- Equality und typisierte IDs;
- klassische Aggregate-Invariants;
- neues Event wird genau einmal angewandt;
- Replay erzeugt keine uncommitted Events;
- fehlender Apply-Handler scheitert eindeutig;
- Reihenfolge und Version nach Replay;
- Commitfehler erhält uncommitted Events für kontrollierte Behandlung.

**Gate G2:** Die taktische API wird vor jedem Providerumbau abgenommen.

## 7. Phase F3: Eventverträge und Evolution

### F3.1 Event Envelope

Der Envelope enthält mindestens:

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
Actor optional
TurnId optional
Payload
```

Globale Position wird erst beim produktiven Commit vergeben. Domain Events kennen keine Storeposition.

### F3.2 Event Registry

- expliziter stabiler Type Key;
- genau eine CLR-Zuordnung je aktuellem Type Key;
- doppelte Keys führen beim Start zum Fehler;
- unbekannte Keys ergeben einen klassifizierten Deserialisierungsfehler;
- keine neuen persistierten Assembly Qualified Names.

### F3.3 Codec und Upcasting

- `System.Text.Json` ist die bevorzugte Standardimplementierung;
- Provider speichern Payload plus Typ- und Schemametadaten;
- Upcaster arbeiten von einer bekannten Schema-Version zur nächsten;
- Originalevents werden nicht in-place umgeschrieben;
- historische JSON-Fixtures sichern Kompatibilität.

### F3.4 Kompatibilität

Vorhandene persistierte MSSQL-Daten sind nur dann zu migrieren, wenn es einen realen Bestand gibt, der erhalten werden muss. Ohne solchen Bestand genügt eine dokumentierte Breaking Change. Keine spekulative Universalmigration.

**Gate G3:** Persistenzformat und Evolutionsstrategie ausdrücklich abnehmen.

## 8. Phase F4: Async, Cancellation, Fehler und DI

### F4.1 I/O-Contracts

`CancellationToken` wird an allen I/O-Grenzen letzter Parameter mit Default nur dort, wo API-Kompatibilität sinnvoll ist. Reine Domainmethoden bleiben synchron.

### F4.2 Fehlerklassifikation

Mindestens getrennt:

```text
Validation
Domain Rejection
Stream Not Found
Stream Already Exists
Concurrency Conflict
Unknown Event Type
Unsupported Schema Version
Serialization Failure
Transient Provider Failure
Permanent Provider Failure
Cancellation
```

### F4.3 DI und Dispatch

- normale Laufzeit ohne Service Locator;
- explizite Registrierung;
- Startup Validation für fehlende und doppelte Handler;
- Scoped Dependencies korrekt;
- Reflection höchstens als validierte Registrierungsunterstützung beim Start;
- Dispatcher ohne versteckte globale Containerreferenz.

### F4.4 Tests

- Cancellation vor I/O;
- Cancellation während Provideroperation;
- Cancellation wird nicht als Domainfehler verpackt;
- doppelte Handlerregistrierung;
- fehlender Handler;
- Scoped Lifetime;
- parallele Dispatches ohne globalen Zustand.

**Gate G4:** Application-kompatible Kerncontracts und Fehlersemantik abnehmen.

## 9. Phase F5: In-Memory-Provider und Contract Suite

### F5.1 Produktionsnahe Semantik

- thread-safe;
- atomare Expected-Version-Prüfung;
- eindeutige EventIds;
- stabile Streamreihenfolge;
- committed Feed in globaler Reihenfolge;
- Cancellation;
- keine Publication vor Commit;
- keine stillen Best-Effort-Abweichungen.

### F5.2 Provider Contract Suite

Ein abstraktes Testset prüft jeden Event-Store-Provider:

- neuer Stream;
- Append eines und mehrerer Events;
- Laden ab Version;
- falsche Expected Version;
- parallele Writer;
- doppelte EventId;
- Cancellation;
- globale Position;
- Feed ab Position;
- unveränderliche gespeicherte Payload;
- Fehler- und Retryklassifikation.

Separate Contracts entstehen bei Bedarf für klassische Aggregate, Snapshots und Projection Checkpoints.

**Gate G5:** Die Contract Suite wird als normative Providersemantik abgenommen.

## 10. Phase F6: Produktprovider-Entscheidung

### F6.1 Spike A: PostgreSQL-Adapter

Der Spike beweist:

- atomaren Append mit Expected Version;
- monotone globale Position;
- Stream Read;
- committed Feed;
- Transaktions- und Concurrencyverhalten;
- Migration, Backup und Restore;
- Betrieb über Testcontainers.

### F6.2 Spike B: externer Event Store, nur wenn ernsthafter Kandidat

Ein externer Store wird nur gesondert untersucht, wenn er einen konkreten Vorteil verspricht. Bewertet werden zusätzliche Betriebsabhängigkeit, ARM64-Verfügbarkeit, Backup, Clientreife, Projektionseinbindung und Homelab-Aufwand.

### F6.3 Entscheidung

PostgreSQL ist Default-Hypothese. Ein externer Store gewinnt nur mit belegbarem Mehrwert. Für den MVP wird genau ein Produktprovider fertiggestellt.

**Gate G6:** ADR mit Entscheidung, Risiken und verworfenen Alternativen.

## 11. Phase F7: Produktprovider

### F7.1 Schema und Migrationen

Mindestens logisch:

```text
event_streams
events
snapshots optional
projection_checkpoints
```

Schema, Indizes und Constraints werden aus den Contractanforderungen abgeleitet, nicht aus Bequemlichkeit des ORM.

### F7.2 Append

- eine Transaktion;
- Stream anlegen oder sperren;
- Expected Version atomar prüfen;
- Batch von Events schreiben;
- globale Positionen vergeben;
- Commit;
- erst danach als committed sichtbar.

### F7.3 Tests

- gesamte Contract Suite;
- Testcontainers auf Linux;
- echte Parallelität mit mehreren Connections;
- Prozessabbruch und Rollback;
- Migration von leerer und vorheriger Schema-Version;
- Backup/Restore-Smoke-Test;
- lange Streams und große Payloads in sinnvollen Grenzen.

**Gate G7:** Produktive Persistenz freigeben.

## 12. Phase F8: Projections und Recovery

### F8.1 Feed und Checkpoint

- monotoner Cursor;
- Checkpoint je Projection Name und Version;
- Eventanwendung und Checkpointupdate in derselben Read-Store-Transaktion, wo möglich;
- idempotente Wiederholung;
- keine OFFSET-basierte Navigation.

### F8.2 Fehlerbehandlung

- betroffene Projection stoppt oder geht in sichtbaren Retryzustand;
- andere Projections laufen unabhängig;
- Domain Commit bleibt gültig;
- Admin kann Retry und Rebuild auslösen;
- Poison Event wird diagnostizierbar, nicht still übersprungen.

### F8.3 Rebuild

- Read Model verwerfen;
- Checkpoint zurücksetzen;
- ab Position null replayen;
- Fortschritt sichtbar;
- Ergebnis gegen inkrementelle Projektion vergleichen.

**Gate G8:** CQRS-Grundlage freigeben.

## 13. Phase F9: Snapshots

F9 beginnt nur bei gemessenem Bedarf oder wenn die bestehende Framework-API ohne großen Zusatzaufwand konsistent gehalten werden muss.

- Snapshot referenziert Streamversion;
- Snapshotformat versioniert;
- inkompatibler Snapshot wird verworfen;
- vollständiges Replay bleibt möglich;
- Snapshotfehler ändert keinen Eventcommit;
- Äquivalenztest Replay gegen Snapshot plus Restevents.

## 14. Phase F10: Paketierung und Release

### F10.1 Paketqualität

- Paketabhängigkeiten minimal und korrekt;
- README und Package Description;
- Lizenz und Repositorymetadaten;
- Symbols und SourceLink;
- XML-Dokumentation;
- keine Examples, Secrets oder Buildreste im Paket;
- `dotnet nuget verify`, soweit Signierung eingesetzt wird;
- Consumer-Smoke-Test in leerem Projekt.

### F10.2 Versionierung

Empfehlung:

```text
0.x.y-alpha.N    Modernisierung und API-Exploration
0.x.y-rc.N       Playground-kompatibler Release Candidate
1.0.0            erst nach stabilisierter öffentlicher API
```

Falls das Framework bereits veröffentlichte stabile Versionen besitzt, wird stattdessen SemVer kompatibel zur bestehenden Historie fortgeführt. Eine künstliche Rückkehr auf `0.x` wäre dann falsch.

### F10.3 Feeds

- lokaler Ordnerfeed für unmittelbare Entwicklung;
- privater oder GitHub-basierter Feed für reproduzierbare Vorabversionen;
- NuGet.org nur bei bewusster öffentlicher Veröffentlichung;
- Playground pinnt eine konkrete Version und niemals `*`.

### F10.4 Release Gate

- Unit-, Contract-, Integration- und Recovery-Suites grün;
- Package Consumer Smoke Test grün;
- Release Notes und Migration Guide;
- bekannte Einschränkungen;
- getaggter Commit;
- ausdrückliche Freigabe `READY FOR PLAYGROUND`.

## 15. Codex-Zuschnitt

Jede Unteretappe wird als eigener Auftrag formuliert. Ein Auftrag enthält maximal einen Hauptgrund für Änderungen.

Beispielhafte Reihenfolge der ersten Aufträge:

```text
1. F0.1 und F0.3: Baseline untersuchen, nichts ändern
2. Devcontainer und reproduzierbare Befehle ergänzen
3. .NET-10-Zielmigration ohne Package-Großupdate
4. Central Package Management einführen
5. Nullable-Befunde klassifizieren
6. Nullable in einem Paket härten
7. Pack- und Consumer-Smoke-Test aufbauen
```

Die vollständigen Auftragsformulierungen entstehen jeweils erst nach Sichtung des vorherigen Ergebnisses.

## 16. Kritische Reviewpunkte

- Wird Core frei von Npgsql, EF Core, ASP.NET und Hosting gehalten?
- Bleiben Domain Events frei von Infrastrukturmetadaten?
- Ist Expected Version im Provider wirklich atomar?
- Verhält sich In-Memory nicht großzügiger als Produktion?
- Kann jedes Read Model vollständig rebuildet werden?
- Bleibt Event Evolution ohne CLR-Namensbindung möglich?
- Sind Packagegrenzen sinnvoll oder nur historisch?
- Ist eine Breaking Change ehrlicher als eine missverständliche Kompatibilitätsschicht?

## 17. Abschluss

Die Frameworkmodernisierung ist beendet, wenn nicht nur der Code modern aussieht, sondern ein externes Consumerprojekt die gepackten Artefakte unter realer Persistenz-, Concurrency-, Projection- und Recovery-Semantik erfolgreich nutzt.

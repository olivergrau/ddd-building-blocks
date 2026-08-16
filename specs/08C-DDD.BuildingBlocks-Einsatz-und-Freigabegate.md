# Persona Simulation Playground

## Arbeitspaket 8C: DDD.BuildingBlocks Einsatz- und Freigabegate

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16

## 1. Zweck

Dieses Dokument ergänzt die Frameworkanalyse `08A` und den Modernisierungsplan `08B`. Es definiert, wann DDD.BuildingBlocks als belastbare Grundlage für den Playground freigegeben wird und welche Entscheidungen bis dahin bewusst offenbleiben.

## 2. Verwendungsentscheidung

DDD.BuildingBlocks wird als taktische DDD-Grundlage verwendet und vor Beginn der Playground-Implementierung gezielt modernisiert.

Übernommen werden insbesondere:

- Entity, EntityId, ValueObject und Aggregate Root;
- klassische und eventgesourcte Aggregate;
- Command- und Eventverarbeitung;
- Provider-Schnittstellen;
- Event Store, Snapshot- und Projection-Konzepte;
- Testunterstützung.

Es wird keine konkurrierende zweite Building-Blocks- oder Event-Sourcing-Basis im Playground aufgebaut.

## 3. Reihenfolge

```text
F0 Baseline reproduzieren
-> F1 bis F5 Core modernisieren und härten
-> F6 Produktprovider entscheiden
-> F7 Produktprovider implementieren
-> F8 Projection und Recovery nachweisen
-> optional F9 Snapshot-Härtung
-> F10 Release und ausdrückliche Freigabe
-> erst danach Playground Foundation
```

## 4. Freigabekriterien

### Build und Plattform

- .NET 10 Build auf Linux reproduzierbar;
- keine unerklärten Compilerwarnungen;
- Nullable-Strategie konsistent;
- Package-Abhängigkeiten aktuell und begründet;
- Beispiele bauen oder sind nachvollziehbar als historisch markiert.

### Taktisches Modell

- klassische und eventgesourcte Aggregate sind sauber getrennt;
- uncommitted Events und Replay sind gekapselt;
- Apply ist deterministisch;
- Versionssemantik ist dokumentiert;
- taktische Typen erzwingen keine Infrastrukturabhängigkeit im Playground-Domainmodell.

### Event Contracts

- stabile Event-Type-Keys statt persistierter CLR-Namen;
- Schema-Versionierung und Registry;
- Event Envelope trennt Metadaten von fachlicher Payload;
- Upcasting-Pfad mindestens exemplarisch getestet;
- Correlation und Causation sind möglich.

### Provider

- providerneutrale Contracts sind festgelegt;
- In-Memory-Provider ist thread-safe und produktionsnah in seiner Semantik;
- genau ein Produktprovider ist für die erste Nutzung ausgewählt;
- beide bestehen dieselbe Kern-Contract-Suite;
- Produktprovider besteht zusätzliche reale Integrations-, Concurrency- und Recovery-Tests.

### Projections und Recovery

- committed Event Feed;
- Checkpoint atomar oder anderweitig korrekt gekoppelt;
- idempotente Verarbeitung;
- Retry und sichtbarer Fehlerzustand;
- vollständiger Rebuild;
- Projection Failure verändert keinen erfolgreichen Domain Commit.

### API-Qualität

- Async und Cancellation an I/O-Grenzen;
- keine Service-Locator-Abhängigkeit im normalen Laufzeitpfad;
- Conflict, Validation und Infrastructure Failure unterscheidbar;
- DI-Registrierung explizit und testbar.

### Nachweise

- Unit-Tests;
- Provider Contract Tests;
- reale Produktprovider-Integrationstests;
- Migration und Recovery;
- Beispiel oder Referenzanwendung für den vorgesehenen Happy Path;
- kurze Upgrade- und Usage-Dokumentation.

## 5. Produktprovider-Entscheidung

Die Technik wird nicht in der Spezifikationsphase vorweggenommen. Am Gate F6 werden mindestens geprüft:

- benötigte Event-Store-Semantik;
- atomare Expected-Version-Prüfung;
- committed Feed und Projection-Anbindung;
- Migration, Backup und Restore;
- Betrieb im Homelab und k3s;
- Testcontainers-Unterstützung;
- Wartungs- und Upgradeaufwand;
- zusätzlicher Infrastrukturbedarf.

PostgreSQL ist die bevorzugte Ausgangshypothese, weil es ohnehin für klassische Aggregate und Read Models gesetzt ist. Ein externer Event Store ist nur dann vorzuziehen, wenn er einen belegbaren funktionalen oder betrieblichen Vorteil liefert, der die zusätzliche Komplexität rechtfertigt.

## 6. Release-Artefakt

Die Freigabe erfolgt über eine eindeutig versionierte Frameworkversion. Der Playground referenziert keine zufällige lokale Arbeitskopie und keinen unmarkierten Branchstand.

Das Release enthält:

- Versionsnummer oder Tag;
- Release Notes;
- bekannte Einschränkungen;
- Migrationshinweise;
- getestete .NET- und Provider-Versionen;
- Referenz auf grüne Testläufe.

## 7. Stop- und Eskalationsregeln

Blockierend sind:

- nicht reproduzierbarer Baseline-Build;
- unklare oder inkonsistente Aggregate-Versionierung;
- keine atomare Concurrency-Prüfung im gewählten Produktprovider;
- persistierte Abhängigkeit von CLR-Typnamen ohne Migrationspfad;
- nicht rebuildbare Projektionen;
- Provider-Contracts, die In-Memory und Produktprovider semantisch auseinanderlaufen lassen;
- Framework-API, die Infrastruktur in das Playground-Domainmodell zwingt.

Nicht blockierend, aber zu dokumentieren sind:

- kosmetische API-Namen;
- zusätzliche Convenience APIs;
- adaptive Snapshot Policy;
- weitere Provider;
- Optimierungen ohne gemessenen Bedarf.

## 8. Formale Freigabe

Nach F10 wird ein kurzer Bericht vorgelegt mit:

```text
verwendeter Commit und Release
erfüllte Kriterien
Testnachweise
bekannte Einschränkungen
gewählter Produktprovider
offene spätere Erweiterungen
```

Der Playground beginnt erst nach Olivers ausdrücklicher Entscheidung:

```text
READY FOR PLAYGROUND
```

## 9. Abnahmekriterien für Arbeitspaket 8

- Analyse, Modernisierungsplan und Releasegate bilden einen vollständigen Ablauf.
- Frameworkmodernisierung steht vor Playground-Code.
- Produktprovider bleibt bis F6 technologieoffen.
- PostgreSQL ist Präferenz, aber keine unbegründete Vorabfestlegung des Event Stores.
- reale Integrations- und Contract-Tests sind verpflichtend.
- es existiert genau eine freigegebene taktische Grundlage.


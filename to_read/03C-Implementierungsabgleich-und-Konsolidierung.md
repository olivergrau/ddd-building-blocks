# Persona Simulation Playground

## Arbeitspaket 3C: Implementierungsabgleich und Konsolidierung

**Status:** OPEN, fortlaufendes Arbeitsdokument  
**Version:** 0.1  
**Datum:** 2026-08-16  
**Zweck:** Erkenntnisse aus C#-Modellierung und Tests kontrolliert in die Verträge zurückführen

## 1. Rolle dieses Dokuments

3C ist kein vorgelagertes Big-Design-Dokument. Es wird während der ersten Domain-Etappen geführt.

Jede Abweichung von 3A oder 3B muss hier begründet werden. Eine Codeabweichung gilt nicht automatisch als neue Entscheidung.

## 2. Arbeitszyklus

```text
Vertrag aus 3A/3B auswählen
        |
kleinste C#-Modellierung erstellen
        |
Given-When-Then-Test schreiben
        |
Widerspruch oder Reibung beobachten
        |
Entscheidung dokumentieren
        |
3A/3B bestätigen oder gezielt ändern
        |
Stop und Review durch Oliver
```

## 3. Änderungsprotokoll

| ID | Datum | Betroffener Vertrag | Beobachtung | Optionen | Entscheidung | Auswirkung | Abgenommen |
|---|---|---|---|---|---|---|---|
| C-001 | offen | offen | offen | offen | offen | offen | nein |

## 4. Zu validierende Hypothesen

| Hypothese | Test/Spike | Erfolgskriterium | Status |
|---|---|---|---|
| `READY` ist als `CanStart` ausreichend | Session Lifecycle Tests | keine künstlichen Readiness-Events nötig | offen |
| Ein Session Aggregate bleibt handhabbar | Rehydration mit Participants und Relationships | klare Invariants, akzeptable Testkomplexität | offen |
| InitialRelationships passen in `ParticipantAdded` | Event Contract Test | historisch stabil, kein übergroßer Payload | offen |
| `DoNothing` benötigt kein Domain Event | Runtime/Application Test | Turnabschluss bleibt nachvollziehbar | offen |
| Der alleinige Event-Sourcing-Root bleibt fokussiert | Framework-Spike | kein Conventional Root und keine konsumlose Oberabstraktion | entschieden 2026-08-16 |
| Event Envelope bleibt außerhalb der fachlichen Payload | Serialization Test | Domain Events ohne Infrastrukturfelder | offen |
| ExpectedVersion schützt stale Turns vollständig | Concurrency Integration Test | alter Turn kann nicht committen | offen |
| Projection Checkpoint ist rebuildbar | PostgreSQL Integration Test | Reset und Replay ergeben identisches Read Model | offen |

## 5. Verbindliche erste Code-Etappen für 3C

### C1: Framework-Kerntypen als Spike

- `EventSourcedAggregateRoot<TKey>` als alleiniger Root-Typ;
- Migrationsstrategie für den bisherigen Namen `AggregateRoot<TKey>`;
- Event Envelope;
- Versionskonvention;
- kein PostgreSQL in dieser Etappe.

**Stop-Punkt:** API und Tests gemeinsam prüfen.

### C2: Event-sourced Persona-Minimalmodell

- `PersonaId`;
- `Persona`;
- eine immutable `PersonaVersion`;
- neue Version erzeugen;
- archivieren;
- fachlich grobkörnige Events und Replay-Tests.

**Stop-Punkt:** Prüfen, ob Frameworktypen helfen oder unnötig behindern.

### C3: Minimale Session-State-Machine

- `SessionCreated`;
- minimale Konfiguration;
- `CanStart`;
- `SessionStarted`;
- `SessionPaused`;
- `SessionResumed`;
- `SessionCompleted`;
- `SessionAborted`;
- Given-When-Then-Tests.

**Stop-Punkt:** Lifecycle fachlich abnehmen, bevor Participants folgen.

### C4: Participants und Versionsbindung

- Add/Remove in Draft;
- doppelte Persona ablehnen;
- PersonaVersion nach Start unveränderlich;
- Enter/Leave als späterer separater Schritt.

### C5: ParticipantAction Mapping

- zunächst `Speak` und `DoNothing`;
- kein vLLM;
- Fake Application Input;
- stale ExpectedVersion Test.

### C6: Eventstore-Contract

- In-Memory-Provider;
- atomarer Expected-Version-Test;
- Event Envelope;
- Rehydration;
- Idempotency-Grundregel.

Erst danach folgt PostgreSQL als eigene Framework-Etappe.

## 6. Reviewfragen nach jeder Etappe

1. Ist die fachliche Sprache im Code klarer oder unklarer geworden?
2. Erzwingt das Framework eine ungewollte technische Form?
3. Ist eine Invariant im Aggregate oder versehentlich im Handler gelandet?
4. Ist ein Event wirklich eine fachliche Tatsache?
5. Enthält eine Payload rekonstruierbare oder technische Daten?
6. Kann Replay deterministisch denselben Zustand erzeugen?
7. Ist ein Fehler Domain, Application oder Infrastructure?
8. Wurde Scope aus Bequemlichkeit erweitert?
9. Muss 3A oder 3B geändert werden?
10. Ist der Stand klein genug für eine echte manuelle Abnahme?

## 7. Statusregeln

```text
OPEN
  Entscheidung oder Test fehlt

PROVISIONAL
  im Code erprobt, noch nicht abgenommen

ACCEPTED
  fachlich und technisch abgenommen

SUPERSEDED
  durch dokumentierte spätere Entscheidung ersetzt
```

## 8. Abschlusskriterien für Arbeitspaket 3

Arbeitspaket 3 wird erst `READY`, wenn:

- 3A fachlich abgenommen ist;
- 3B semantisch abgenommen ist;
- mindestens C1 bis C3 als Code und Tests durchlaufen wurden;
- gefundene Widersprüche in 3C entschieden wurden;
- Session Lifecycle und Versionssemantik bestätigt sind;
- Event Envelope und Expected-Version-Verhalten bestätigt sind;
- kein kritischer Frameworkblocker offen ist;
- weitere Implementierung in kleine, kontrollierte Etappen überführt werden kann.

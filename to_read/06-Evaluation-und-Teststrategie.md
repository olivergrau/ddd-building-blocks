# Persona Simulation Playground

## Arbeitspaket 6: Evaluation- und Teststrategie

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16

## 1. Ziel

Dieses Dokument definiert, wie technische Korrektheit, Betriebsstabilität und die Qualität der Persona-Simulation nachgewiesen werden. Hohe Testabdeckung bedeutet dabei nicht nur eine hohe Line- oder Branch-Coverage. Entscheidend ist, dass die risikoreichen fachlichen und technischen Pfade mit der jeweils passenden Testtiefe geprüft werden.

Technische Regression und Behavioral Evaluation werden getrennt behandelt. Ein technisch korrektes System kann langweilige oder zusammenfallende Personas erzeugen. Umgekehrt darf ein überzeugender Beispieldialog keine Fehler in Persistenz, Concurrency oder Recovery verdecken.

## 2. Verbindliche Grundsätze

1. Tests werden mit dem Produktionscode entwickelt, nicht nachträglich ergänzt.
2. Jede Implementierungsetappe benennt ihre erforderlichen Unit-, Integrations- und gegebenenfalls End-to-End-Tests.
3. Ein Mock ersetzt nur eine Grenze, die für den konkreten Test nicht Gegenstand der Prüfung ist.
4. Zu integrierende Komponenten werden im Integrationstest real ausgeführt.
5. End-to-End-Tests verwenden so viel reale Infrastruktur wie praktikabel.
6. Der In-Memory-Provider muss dieselbe relevante Contract Suite wie der Produktprovider bestehen.
7. Provider-spezifische Eigenschaften werden zusätzlich mit dem realen Provider geprüft.
8. Nichtdeterministische Modellantworten werden nicht als gewöhnliche binäre Unit-Tests behandelt.
9. Fehlerszenarien, Neustarts und Recovery sind regulärer Testumfang.
10. Deaktivierte oder flakige Tests gelten als sichtbare Qualitätsprobleme.

## 3. Testpyramide und Testrealismus

| Ebene | Zweck | Real ausgeführt | Ersetzt oder kontrolliert |
|---|---|---|---|
| Unit | einzelne Domainregel oder reine Komponente | Testgegenstand | alle externen Collaborators |
| Component | ein Modul mit mehreren internen Klassen | komplettes Modul | externe Ports |
| Integration | Zusammenspiel ausgewählter Komponenten | alle Komponenten der betrachteten Integration | nur nicht betrachtete Außengrenzen |
| Contract | identisches Verhalten mehrerer Provider | jeweiliger Provider | andere Systemteile |
| End-to-End | vollständiger Benutzer- und Runtime-Fluss | API, Runtime, Persistenz, Projektionen und möglichst echte Infrastruktur | nur bewusst ausgeschlossene oder nicht verfügbare Systeme |
| Behavioral Evaluation | Qualität von Persona-Verhalten | Context Builder, Prompting und Modell nach Testprofil | Zufall wird kontrolliert, Judge ist nur ein Signal |

Die Zahl der End-to-End-Tests bleibt kleiner als die Zahl der Unit-Tests. Das ist keine Abwertung. End-to-End-Tests sind teurer, langsamer und diagnostisch unschärfer, aber für die zentralen Produktpfade unverzichtbar.

## 4. Unit-Tests

Unit-Tests laufen ohne Netzwerk, Datenbank, Dateisystem oder echten Zeitablauf. Clock, Random Source, Inference, Persistenz und Publication werden kontrolliert ersetzt.

### 4.1 Domain

- Aggregate-Invariants;
- erlaubte und abgelehnte Zustandsübergänge;
- Value Objects und typisierte IDs;
- deterministisches Event Apply;
- Rehydration aus Events;
- Persona-, Scenario- und Relationship-Eventfolgen sowie Versionierungsregeln;
- Session Lifecycle;
- Participant- und Relationship-Veränderungen;
- terminale Zustände;
- stale Expected Version als fachlich geeignete Ablehnung.

Für eventgesourcte Aggregate gilt bevorzugt Given-When-Then:

```text
Given: historische Events
When:  Command oder Domainoperation
Then:  neue Events oder definierte Ablehnung
```

### 4.2 Application und Orchestrierung

- Human Input hat die festgelegte Priorität;
- genau ein Participant wird gewählt;
- nicht berechtigte Actors werden ausgeschlossen;
- `DoNothing` und Idle sind kontrolliert;
- ein Inference-Ergebnis wird vor Commit erneut validiert;
- stale Ergebnisse werden verworfen;
- Retry-Entscheidungen unterscheiden transient, invalid und permanent;
- Persistierung geschieht vor Projection und Publication;
- UI-Verbindungen beeinflussen die Runtime nicht.

### 4.3 Context Builder

- deterministische Blockreihenfolge;
- persona-spezifische Sicht;
- Originalaussagen relevanter Teilnehmer bleiben im Verlauf enthalten;
- stabile und dynamische Blöcke sind getrennt;
- Tokenbudget und Kürzungsreihenfolge;
- keine fachliche Abhängigkeit vom KV-Cache;
- gleiche Eingaben erzeugen byte- beziehungsweise tokenstabile Prompts, soweit Template und Tokenizer unverändert sind.

## 5. Integrationstests

Integrationstests definieren vorab ihre Integrationsgrenze. Innerhalb dieser Grenze werden keine Komponenten gemockt.

### 5.1 Framework und Persistenz

- Event Append mit Expected Version;
- parallele Append-Versuche;
- Stream Read und Rehydration;
- Event Envelope, Codec und Upcasting;
- Event-Stream-Persistenz aller fachlichen Aggregate;
- Snapshot Write, Read und Verwerfung;
- Projection Checkpoints;
- idempotente Projektion;
- Retry und Dead-Letter- beziehungsweise Fehlerzustand;
- vollständiger Rebuild;
- Migration, Backup und Restore.

Diese Tests laufen gegen den realen gewählten Produktprovider, vorzugsweise in kurzlebigen Containern. Ein EF-Core-In-Memory-Provider ist kein Ersatz für PostgreSQL-Integrationstests, weil Transaktionen, Concurrency, SQL-Semantik und Constraints abweichen.

### 5.2 API und Server

Mit einem echten ASP.NET-Core-Testhost werden geprüft:

- Authentifizierung und unautorisierter Zugriff;
- Command- und Query-Endpunkte;
- Validation und Fehlerabbildung;
- Optimistic-Concurrency-Konflikte;
- mehrere Tabs als mehrere Clients auf denselben Serverzustand;
- SSE Connect, Reconnect und Replay ab Cursor;
- Erreichbarkeit von Admin- und Query-Endpunkten während eines Turnfehlers.

### 5.3 vLLM-Adapter

Zwei Testgruppen werden getrennt:

1. Adapter-Integration gegen einen kontrollierten HTTP-Stub für Protokollfehler, Timeouts, Cancellation und ungültige Structured Outputs.
2. Smoke- und Behavioral-Tests gegen den echten konfigurierten vLLM-Endpunkt.

Der HTTP-Stub beweist Adapterlogik. Nur der echte Endpoint beweist Modell-, Template-, Tokenizer- und Deployment-Kompatibilität.

## 6. Contract Tests

Provider-Contracts werden einmal formuliert und gegen alle Implementierungen ausgeführt.

Verbindliche Contract Suites:

- Event Store;
- Event Store für alle unterstützten Aggregate-Typen;
- Projection Checkpoint Store;
- Snapshot Store, falls aktiviert;
- Inference Client auf Protokollebene;
- Clock und Random Source nur dort, wo mehrere Implementierungen existieren.

Der In-Memory-Provider darf keine großzügigere Semantik besitzen als der Produktprovider. Besonders wichtig sind Expected Version, Reihenfolge, Deduplizierung, Cancellation und Sichtbarkeit committed Events.

## 7. End-to-End-Strategie

### 7.1 E2E lokal und in CI

Produktionsnaher Systemverbund:

```text
Browser oder API-Testclient
        -> echter ASP.NET-Core-Prozess
        -> echte Runtime und Application
        -> echter Produktprovider
        -> echte Projektionen
        -> kontrollierter Inference-Stub
```

Der Inference-Stub ist hier zulässig, wenn der Test deterministisch Session-, Persistenz-, Projection-, API- und UI-Verhalten prüft.

### 7.2 E2E mit realem vLLM

Eine kleinere, explizit gekennzeichnete Suite verwendet zusätzlich den echten vLLM-Endpunkt. Sie prüft:

- strukturierten Output;
- Persona-spezifischen Kontext;
- vollständigen Drei-Persona-Turn;
- Pause, Resume und Human Intervention;
- Verhalten nach vLLM-Neustart;
- Token- und Latenzmetriken;
- Prefix-Cache-Messungen ohne funktionale Cache-Abhängigkeit.

Diese Suite darf wegen Kosten und Laufzeit separat ausgelöst werden. Sie ist vor MVP-Abnahme verpflichtend, aber nicht zwingend bei jedem lokalen Build.

### 7.3 Minimale verbindliche E2E-Szenarien

1. Anwendung starten, anmelden, Persona und Scenario laden, Session konfigurieren und starten.
2. Drei unterschiedliche Personas erzeugen nacheinander persistierte Beiträge.
3. Browser schließen, Session läuft weiter, Browser neu verbinden und Zustand rekonstruieren.
4. Human Message einreichen, laufendes stale Inference-Ergebnis wird nicht committed.
5. Session pausieren, Prozess neu starten und kontrolliert fortsetzen.
6. Projektion löschen, vollständig rebuilden und identisches Read Model erhalten.
7. Zweiter Tab sendet veralteten Command und erhält einen verständlichen Konflikt.
8. vLLM fällt aus, UI und administrative Steuerung bleiben erreichbar.

## 8. Behavioral Evaluation

### 8.1 Versionierte Fixtures

Fixtures enthalten:

- Persona-Versionen;
- Scenario-Version;
- Relationships;
- Interaction-Mode-Konfiguration;
- Modell- und Samplingprofil;
- Seeds, soweit technisch wirksam;
- erwartete qualitative Merkmale;
- verbotene oder unerwünschte Muster.

### 8.2 Kernprüfungen

| Prüfung | Fragestellung | Auswertung |
|---|---|---|
| Persona Identity | handelt und spricht die Persona erkennbar eigenständig? | Merkmalsrubrik plus Human Review |
| Blind Classification | ist der Urheber ohne Namen erkennbar? | Klassifikationsrate, Confusion Matrix |
| Single Trait Variation | verändert nur ein Trait plausibel das Verhalten? | paarweiser Vergleich |
| Relationship Sensitivity | reagiert A auf B anders als auf C? | kontrollierte Gegenüberstellung |
| Context Persistence | bleiben frühere Aussagen und Konflikte wirksam? | Fakten- und Positionsprüfung |
| Scenario Fidelity | bleibt Verhalten im gesetzten Szenario? | Rubrik und Regelverstöße |
| Persona Collapse | nähern sich Personas über längere Läufe ungewollt an? | Stil- und Entscheidungsdivergenz |
| Intervention | wirkt Human Input nachvollziehbar auf Folgeturns? | Vorher-Nachher-Vergleich |

LLM-as-Judge darf Rubriken skalieren und Auffälligkeiten markieren. Es ist weder alleinige Wahrheit noch Ersatz für fest definierte Faktenchecks und Human Review.

### 8.3 Statistik und Modellvarianz

Ein einzelner Lauf beweist keine Behavioral Quality. Für wichtige Baselines werden mehrere Läufe mit dokumentierter Konfiguration ausgeführt. Berichtet werden Verteilung, Ausreißer und Fehlermuster, nicht nur ein Mittelwert.

Schwellenwerte werden nach einer ersten Baseline empirisch festgelegt. Vorher erfundene Prozentwerte würden eine Präzision vortäuschen, die noch nicht vorhanden ist.

## 9. Coverage Policy

Coverage ist ein Diagnoseinstrument und Gate, aber kein Selbstzweck.

Empfohlene Startwerte:

| Bereich | Zielrichtung |
|---|---|
| Domain und reine Application Policies | sehr hoch, Branch Coverage besonders relevant |
| Event Apply, Rehydration und Concurrency-Regeln | praktisch vollständig für definierte Pfade |
| Infrastructure Adapter | hohe risikobasierte Abdeckung plus reale Integration |
| API Mapping | vollständige Abdeckung der öffentlichen MVP-Verträge |
| UI | kritische Flows und Zustandslogik, nicht triviales Markup |
| generierter oder rein deklarativer Code | nicht künstlich hochtesten |

Konkrete Prozentgates werden nach Framework-Baseline und erstem Vertical Slice festgelegt. Bis dahin gilt: Jeder bekannte risikoreiche Branch benötigt einen Test, unabhängig vom Gesamtscore.

## 10. Testausführung

Vorgesehene Kategorien:

```text
Unit
Architecture
Component
Contract.InMemory
Integration.PostgreSql
Integration.Http
EndToEnd.StubInference
EndToEnd.RealVllm
Behavioral
Performance
Recovery
```

Schnelle Suites laufen bei jedem Commit. Containerbasierte Integrationstests laufen mindestens vor Abnahme einer Etappe und in CI. Real-vLLM-, Behavioral- und längere Recovery-Suites laufen gezielt sowie vor relevanten Gates.

## 11. Testdaten, Diagnose und Reproduzierbarkeit

Jeder fehlgeschlagene Lauf soll mindestens erfassen:

- Test- und Fixture-Version;
- SessionId, TurnId, CorrelationId;
- Modell- und Samplingkonfiguration;
- relevante Eventfolge;
- Promptblock-Metadaten und Hashes;
- strukturierte Modellantwort;
- Validation- und Retry-Ergebnis;
- Provider- und Schema-Version;
- Zeit- und Tokenmetriken.

Keine Chain of Thought wird verlangt, gespeichert oder angezeigt.

## 12. Qualitätsgates

| Gate | Mindestnachweis |
|---|---|
| Framework Ready | Framework-Unit-, Contract- und Produktprovider-Integrationstests grün |
| Domain Ready | Invariants, Lifecycle und Replay vollständig geprüft |
| CQRS Ready | Projection, Checkpoint, Rebuild und Concurrency real integriert |
| Fake Vertical Slice | zentraler E2E-Pfad mit Stub-Inference grün |
| Inference Ready | echter vLLM Smoke-Test und Structured Output stabil |
| Backend MVP | Recovery-, Multi-Tab- und Failure-E2E grün |
| MVP | technischer E2E plus akzeptierte Behavioral Baseline |

## 13. Bewusst vertagte Details

- endgültige Coverage-Prozentwerte;
- konkrete Testframework-Erweiterungen neben dem etablierten .NET-Teststack;
- endgültige CI-Plattform;
- finale Behavioral-Schwellenwerte;
- Lasttests für Multi-User- oder Multi-Session-Betrieb;
- zweite Modellfamilie als verpflichtende Matrix.

## 14. Abnahmekriterien für Arbeitspaket 6

- Unit-, Integration-, Contract- und End-to-End-Grenzen sind eindeutig.
- Zu integrierende Komponenten werden in Integrationstests nicht gemockt.
- E2E verwendet möglichst reale Infrastruktur und begründet verbleibende Doubles.
- PostgreSQL beziehungsweise der gewählte Produktprovider wird real getestet.
- technisches Verhalten und Behavioral Quality sind getrennte Gates.
- Recovery, mehrere Tabs und unabhängige Serverruntime sind explizit geprüft.
- hohe Testabdeckung wird risikobasiert und nicht nur numerisch verstanden.

# Persona Simulation Playground

## Arbeitspaket 5A: Detaillierter Etappenplan

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16  
**Ergänzt:** `05-Inkrementeller-Implementierungs-und-Abnahmeplan.md`

## 1. Abstraktionsebene

Dieser Plan zerlegt die Umsetzung genauer, ohne zukünftige Codex-Aufträge, Dateilisten oder Signaturen vorzeitig festzuschreiben. Vor jeder Etappe entsteht ein Just-in-time-Brief auf Basis des dann realen Repositoryzustands.

Jede Etappe soll typischerweise einen überschaubaren Review ergeben. Wenn ein Schritt mehrere unabhängige Modellentscheidungen enthält, wird er vor Übergabe weiter geteilt.

## 2. Wiederkehrender Ablauf je Etappe

```text
Brief vorbereiten
-> Definition of Ready prüfen
-> Codex implementiert begrenzten Scope
-> automatisierte Tests
-> Diff, Architektur und Verhalten sichten
-> Dokumentabweichungen konsolidieren
-> Oliver akzeptiert oder fordert Korrektur
-> erst danach nächste Etappe
```

## 3. Track F: DDD.BuildingBlocks

| ID | Ergebnis | Testfokus | Stop-Punkt |
|---|---|---|---|
| F0 | reproduzierte Baseline | vorhandene Suite, Buildmatrix | Altfehler klassifizieren |
| F1a | zentrale Buildkonfiguration und .NET 10 | Restore und Compile Linux | Packageänderungen prüfen |
| F1b | Nullable und Warnungsbereinigung | Baseline-Regressionssuite | keine semantischen Nebenänderungen |
| F2a | taktische Basistypen geprüft | Equality, IDs, Invariants | öffentliche API sichten |
| F2b | alleiniger Event-Sourcing Root geprüft | Apply, Replay, uncommitted Events | Benennung und Versionskonvention abnehmen |
| F3a | Event Envelope und Registry | stabile Type Keys, Roundtrip | Persistenzformat prüfen |
| F3b | Schema Version und Upcasting | historische Fixtures | Migrationsstrategie abnehmen |
| F4a | Async und Cancellation | Abbruch vor und während I/O | API-Breite prüfen |
| F4b | DI und Fehlersemantik | Registration, Conflict Mapping | keine Service-Locator-Pfade |
| F5a | In-Memory-Provider härten | Parallelität und Expected Version | Semantik prüfen |
| F5b | Provider Contract Suite | identische Kernfälle | Suite als Gate akzeptieren |
| F6 | Produktprovider-Spike und Entscheidung | reale Kernfälle | ADR akzeptieren |
| F7a | Produktprovider Append und Read | Concurrency, Idempotency | Persistenzschema prüfen |
| F7b | Migration, Backup und Restore | Containerintegration | Betriebsfähigkeit prüfen |
| F8a | Event Feed und Checkpoints | Reihenfolge, Restart | Projection Contract prüfen |
| F8b | Retry, Rebuild und Admin | Failure Injection | Recovery abnehmen |
| F9 | Snapshots, nur bei Bedarf | Äquivalenz und Verwerfung | Nutzen gegen Aufwand prüfen |
| F10 | Versioniertes Release | Gesamtsuite | `READY FOR PLAYGROUND` |

F0 bis F10 laufen im Framework-Repository. Der Playground erhält erst danach eine freigegebene Version.

## 4. Track P: Playground Foundation

| ID | Ergebnis | Testfokus | Nicht enthalten |
|---|---|---|---|
| P0 | Repository, Solution und Buildkonventionen | Build, Format, Test Discovery | Domainmodell |
| P1 | Projektgrenzen | Architekturtests | produktive Features |
| P2 | API-Host als Composition Root | Host Smoke Test | Geschäftslogik |
| P3 | Konfiguration, Logging, Correlation | Configuration Tests | vollständige Observability |
| P4 | Testprojekte und Kategorien | selektive Testausführung | Behavioral Suite |
| P5 | lokale PostgreSQL-Testumgebung | Connectivity und Isolation | k3s Deployment |

## 5. Track D: Verwaltete Domain Aggregate

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| D0 | gemeinsame IDs, Value Objects, Fehler | Frameworknutzung und Fachsprache |
| D1 | Persona Aggregate | Invariants und Lifecycle |
| D2 | immutable PersonaVersion | Versionierung und Referenzen |
| D3 | Scenario Aggregate | Definition und Validierung |
| D4 | immutable ScenarioVersion | reproduzierbare Sessionbasis |
| D5 | PersonaRelationship Baseline | globale Ausgangsbeziehung |
| D6 | Event-Stream-Adapter und Projektionen | reale Providerintegration |
| D7 | Command-Use-Cases und Queries | Application- und CQRS-Grenzen |

Nach D2, D4 und D5 erfolgt jeweils ein Modellreview. UI-Endpunkte werden nur so weit ergänzt, wie sie den vertikalen Slice benötigen.

## 6. Track S: Session Domain

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| S0 | minimaler Session Root | Erstellung und Identität |
| S1 | atomare Konfigurationsbasis | versionierte Referenzen |
| S2 | Participants und initiale Relationships | Invariants |
| S3 | Lifecycle | Start, Pause, Resume, Complete, Abort |
| S4 | Human und Scenario Actions | fachliche Events |
| S5 | Participant Actions | Speak, DoNothing und Validierung |
| S6 | Replay und lange History | deterministischer Zustand |
| S7 | Concurrency und stale Result | Expected Version und TurnId |

S0 bis S7 konkretisieren die offenen Hypothesen aus Arbeitspaket 3C. Erkenntnisse werden dort zurückgeführt.

## 7. Track Q: CQRS und Read Models

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| Q0 | Session Repository über Framework | Load, Execute, Append |
| Q1 | Session Summary Projection | minimaler Queryzustand |
| Q2 | Session Timeline Projection | geordnete Ereignisse |
| Q3 | Projection Dispatcher | persist-before-project |
| Q4 | Checkpoint und Retry | Restart und Fehler |
| Q5 | Rebuild Administration | wegwerfbare Read Models |
| Q6 | Lag und Diagnose | Betriebsbeobachtung |

Ein Read Model darf nie zum Ersatz für Aggregate Load oder Domainentscheidung werden.

## 8. Track T: Deterministischer Vertical Slice

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| T0 | Interaction-Mode-Abstraktion plus Test Mode | Erweiterungspunkt |
| T1 | Context Builder Minimalversion | originale Aussagen, Persona-Sicht |
| T2 | Fake Inference Client | deterministische Actions |
| T3 | manueller Participant Turn | vollständiger Ablauf |
| T4 | Persist, Projection und Live Signal | Commit-Reihenfolge |
| T5 | stale und Human Priority | Concurrency |
| T6 | E2E mit Stub-Inference | kompletter Backendpfad |

Dieser Track beweist die Architektur vor echter Modellvarianz.

## 9. Track V: Reale Inference

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| V0 | vLLM HTTP-Adapter | Protokoll und Cancellation |
| V1 | Structured Output | Parsing und Schema |
| V2 | fachliche Action Validation | Modelloutput ist Vorschlag |
| V3 | Retry- und Fehlerklassifikation | begrenzte Wiederholung |
| V4 | Context Budgeting | Kürzungsregeln |
| V5 | reale Smoke- und Restart-Tests | Endpoint-Kompatibilität |
| V6 | Cache- und Latenzbaseline | Messung, keine Funktionsabhängigkeit |

## 10. Track R: FreeSociety Runtime

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| R0 | Candidate Eligibility | deterministische Filter |
| R1 | Selection Policy Baseline | nachvollziehbare Wahl |
| R2 | FreeSociety Mode | erster produktiver Mode |
| R3 | Step Mode | genau ein Impuls |
| R4 | automatische Impulse | kontrollierte Schleife |
| R5 | Pause und Human Interrupt | keine stale Commits |
| R6 | Limits und Idle | sichere Begrenzung |
| R7 | Interrupted Turn Recovery | bewusste Benutzerentscheidung |

Andere Interaction Modes bleiben LATER. Die Architektur kennt den Erweiterungspunkt, implementiert aber keine spekulativen Modi.

## 11. Track A: API, Live Updates und Zugriff

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| A0 | Command API | Validierung und Problem Details |
| A1 | Query API | Read Models, Paging |
| A2 | SSE Feed | Cursor und Replay |
| A3 | Diagnoseendpunkte | geschützt, keine Chain of Thought |
| A4 | Single-User-Authentifizierung | genau eine Identität |
| A5 | Multi-Tab Concurrency | Konflikte und Idempotency |
| A6 | Failure Isolation | API bleibt bei Runtimefehler erreichbar |

## 12. Track U: UI

Vor U0 liegt die Technologieentscheidung aus Arbeitspaket 7 vor.

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| U0 | Technologie-Spike | Login, SSE, Conflict, Tests |
| U1 | Shell und Navigation | unabhängiger Client |
| U2 | Session Player Read-only | Feed und Reconnect |
| U3 | Human Control | Commands und Capabilities |
| U4 | Session Setup | versionierte Auswahl |
| U5 | Persona und Scenario Management | funktionale Editoren |
| U6 | Relationships | einfache Pflege |
| U7 | Diagnostics | Context, Selection, Runtime |
| U8 | Browser-E2E | zentrale User Flows |

## 13. Track O: Betrieb und k3s

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| O0 | Liveness, Readiness, Dependency Health | getrennte Bedeutungen |
| O1 | Container Images | reproduzierbarer Build |
| O2 | lokale produktionsnahe Compose-Umgebung | App und Persistenz |
| O3 | k3s Manifeste und Secrets | zentraler Betrieb |
| O4 | Ingress und Auth | kein anonymer Heimnetzzugriff |
| O5 | Persistenz, Backup und Restore | Datenhaltbarkeit |
| O6 | Restart- und Failure-Suite | App, Store und vLLM |
| O7 | Betriebsdiagnose | Logs, Correlation, Projection Lag |

PostgreSQL-Integration beginnt bereits vor O. O betrifft die produktionsnahe Verpackung und den k3s-Betrieb, nicht die erstmalige Datenbankimplementierung.

## 14. Track E: Evaluation und MVP-Abnahme

| ID | Ergebnis | Schwerpunkt |
|---|---|---|
| E0 | versionierte Drei-Persona-Fixture | reproduzierbare Basis |
| E1 | technische Regression | alle schnellen und realen Suites |
| E2 | Behavioral Baseline | Identity, Relationship, Context |
| E3 | längere Collapse-Läufe | Persona-Divergenz |
| E4 | Performance Baseline | Tokens, Latenz, Cache |
| E5 | vollständiges MVP-Szenario | fachliche Abnahme |
| E6 | Scope Closure | MUST zu, SHOULD entschieden |

## 15. Querschnittsregeln

- Security wird nicht erst in O ergänzt. Authentifizierung und Secret Handling beginnen mit dem ersten extern erreichbaren Endpoint.
- Tests sind Bestandteil jeder Etappe. Arbeitspaket 6 bestimmt die Tiefe.
- Dokumentation wird nach jeder bestätigten Modelländerung aktualisiert.
- Observability beginnt minimal mit Correlation und strukturierten Logs und wächst entlang realer Diagnosebedürfnisse.
- Performanceoptimierung folgt Messungen. Korrektheit bleibt cacheunabhängig.
- Keine Etappe darf eine zweite aktive autonome Session oder Multi-Tenancy einführen.

## 16. Vorgesehene größere Freigaben

```text
G0 Baseline
G1 Tactical Core
G2 Framework Ready
G3 Classic Domain
G4 Session Domain
G5 CQRS Ready
G6 Fake Vertical Slice
G7 Inference Ready
G8 Backend MVP
G9 Vertical Product
G10 MVP
```

Zwischenfreigaben können bei einem schlechten Diff verweigert werden, auch wenn Tests grün sind. Tests beweisen nur die geprüften Eigenschaften.

## 17. Was erst im jeweiligen Codex-Brief festgelegt wird

- konkrete Dateien und Namespaces;
- endgültige Methodensignaturen;
- exakte Testnamen;
- konkrete Package-Versionen;
- Migrationsnamen und Tabellendetails;
- Shellbefehle;
- Aufteilung einer Etappe in einzelne Commits;
- Kandidatenwahl nach einem Spike.

## 18. Erster Auftrag

Der erste tatsächliche Auftrag bleibt ausschließlich F0. Danach werden Befund, Warnungen, Teststruktur und technische Schulden gemeinsam geprüft. Erst dann wird F1a formuliert.

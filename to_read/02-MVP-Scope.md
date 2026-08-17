# Persona Simulation Playground

## Verbindlicher MVP-Scope

**Status:** Verbindlich / fachlich abgenommen  
**Version:** 1.0  
**Datum:** 2026-08-15  
**Abgenommen:** 2026-08-15  
**Geltungsbereich:** Funktionsumfang, technische Mindestqualität, bewusste Nichtziele und Abnahme des ersten nutzbaren Produkts

---

## 1. Zweck

Dieses Dokument legt den verbindlichen Umfang des Minimum Viable Product des Persona Simulation Playground fest.

Der MVP ist kein Wegwerfprototyp. Er ist:

> Die erste dauerhaft weiterentwickelbare, fachlich nutzbare Ausbaustufe des Persona Simulation Playground auf der vorgesehenen Zielarchitektur.

Der MVP besitzt einen begrenzten Funktionsumfang. Die implementierten Funktionen beruhen jedoch bereits auf den endgültigen Architekturprinzipien.

Spätere Funktionen sollen durch Ergänzung bestehender fachlicher Variationspunkte entstehen, nicht durch Austausch eines absichtlich kurzlebigen Fundaments.

---

## 2. Zentrale MVP-Hypothese

Der MVP prüft:

> Können drei oder mehr klar definierte LLM-Personas in einer freien sozialen Simulation über mehrere Turns hinweg als unterscheidbare, kontextsensitive und sozial aufeinander reagierende Charaktere erlebt werden, während die Simulation für den Menschen jederzeit nachvollziehbar und steuerbar bleibt?

Die technische Funktionsfähigkeit allein genügt nicht. Der MVP muss sowohl technische Robustheit als auch erste beobachtbare Behavioral Quality erreichen.

---

## 3. Produkt- und Betriebsmodell

### 3.1 Persönliche Single-User-Anwendung

Der MVP ist eine persönliche Single-User-Anwendung.

Er besitzt:

- genau eine konfigurierte berechtigte Benutzeridentität;
- keine Mandantenfähigkeit;
- kein fachliches Benutzer- oder Workspace-Modell;
- keine parallelen Benutzer-Sessions;
- keinen anonymen Zugriff.

Mehrere Browser-Tabs oder eigene Geräte dürfen sich gleichzeitig mit denselben einzigen Credentials verbinden.

### 3.2 Zentral gehostete Anwendung

Der reguläre Betrieb erfolgt im privaten Homelab, bevorzugt im k3s-Cluster.

Die zentrale ASP.NET-Core-Anwendung besitzt:

- Domain Model;
- Application Services;
- Runtime und Orchestrierung;
- Event Store sowie technische und abgeleitete Persistenz;
- Projections und Read Models;
- Context Builder;
- Inference-Koordination;
- Recovery und Diagnostik.

PostgreSQL wird von Anfang an als produktive Datenbank verwendet.

Der vLLM-Endpunkt ist extern und konfigurierbar.

### 3.3 Lokale Entwicklung

Dieselbe Anwendung kann für Entwicklung lokal gestartet werden.

```text
dotnet run
+ lokales PostgreSQL über Docker
+ konfigurierbarer Remote-vLLM-Endpunkt
```

Lokaler Entwicklungsbetrieb und k3s-Deployment verwenden dasselbe fachliche und technische Fundament.

---

## 4. UI- und Runtime-Prinzip

Das Web-UI ist ein unabhängiger Beobachtungs- und Steuerungsclient für die zentral gehostete Anwendung.

```text
Web UI
   |
   | Commands, Queries, SSE
   v
zentrale Persona Playground Anwendung
   |
   +--> PostgreSQL
   +--> vLLM
```

### Das UI darf

- Persona-, Scenario- und Relationship-Pflege anbieten;
- Sessions konfigurieren;
- eine Simulation beobachten;
- Commands zur Steuerung senden;
- Human Messages senden;
- Scenario Events injizieren;
- Runtime-, Selection- und Context-Diagnose visualisieren;
- eigenen temporären Navigations-, Formular- und Darstellungszustand besitzen.

### Das UI darf nicht

- fachliche Source of Truth sein;
- Domain Rules durchsetzen oder ersetzen;
- eine Session besitzen;
- den Lebenszyklus einer Session bestimmen, nur weil ein Browser geschlossen wird;
- Orchestrierung oder Inference koordinieren;
- fachlich relevante Zustände ausschließlich lokal halten.

Eine laufende Session läuft ohne verbundenes UI weiter.

Mehrere Tabs visualisieren denselben zentralen Zustand. Gleichzeitig eintreffende widersprüchliche Commands werden serverseitig über Domain Rules und Optimistic Concurrency behandelt.

---

## 5. Session-Betriebsmodell

Der MVP unterstützt beliebig viele gespeicherte Sessions.

Sessions können insbesondere sein:

```text
DRAFT
RUNNING
PAUSED
COMPLETED
ABORTED
```

Zu einem Zeitpunkt wird genau eine Session autonom abgespielt.

Vor dem Start oder Fortsetzen einer anderen Session muss die aktuelle Session pausiert, abgeschlossen oder abgebrochen werden.

Eine pausierte Session kann zu einem späteren Zeitpunkt aus ihrer autoritativen Persistenz rehydriert und fortgesetzt werden.

Alle Commands, Queries, Events, Turns und SSE-Subscriptions bleiben explizit auf eine `SessionId` bezogen. Die Single-Active-Session-Policy darf nicht zu statischen oder impliziten globalen Domainzuständen führen.

---

## 6. Bekannte Variationspunkte

Der MVP implementiert bekannte fachliche Variationspunkte als saubere Erweiterungsgrenzen. Er liefert jedoch nur die aktuell benötigten konkreten Varianten aus.

| Variationspunkt | MVP-Fundament | Produktive MVP-Implementierung | Später |
|---|---|---|---|
| Interaction Mode | Definition, Strategy, Registry, State und Validierung | `FreeSocietyInteractionMode` | Committee, TalkShow, Debate |
| Inference | Application Port | vLLM Adapter | weitere OpenAI-kompatible Backends |
| Persistenz | DDD.BuildingBlocks Provider-Grenzen | PostgreSQL | weitere Provider nur bei Bedarf |
| Tests | Provider-Abstraktion | In-Memory Provider/Test Doubles | weitere Integrationstestvarianten |
| Read Side | Projection Contracts und Recovery | benötigte MVP-Read-Models | weitere Query-Modelle |
| Actor Selection | Selection Policy | Behavioral Activation Policy | alternative Policies |
| Context Rendering | blockbasierter Renderer | ein versionierter vLLM/Qwen-Renderer | weitere Modellrenderer |
| Behavioral Translation | versionierter Translator | eine deterministische Übersetzung | alternative Trait-Systeme |
| Live Transport | Publication Boundary | SSE | WebSocket nur bei Bedarf |
| Randomness | seedbare Zufallsquelle | Weighted Selection | weitere Strategien |

Die Erweiterungspunkte müssen durch die Domäne oder eine konkrete externe Grenze begründet sein. Es wird keine generische Plugin-Plattform und keine universelle Policy-Sprache implementiert.

---

## 7. MVP-Klassifikation

### 7.1 MUST

`MUST` bezeichnet Funktionen und Qualitätsmerkmale, ohne die der MVP nicht abgenommen wird.

#### Dauerhaftes technisches Fundament

- C# und ASP.NET Core auf Linux;
- domain-zentrierter modularer Monolith;
- getrennte Assemblies für Domain, Application, Infrastructure und API;
- direkte Nutzung der modernisierten DDD.BuildingBlocks;
- Event Sourcing für alle fachlichen Aggregate;
- eigene Event Streams für Session, Persona, Scenario und PersonaRelationship;
- CQRS mit getrennter Write- und Read-Side;
- rekonstruierbare und wegwerfbare Read Models;
- Snapshots als reine Performanceoptimierung;
- Optimistic Concurrency;
- persist-before-publish;
- PostgreSQL als produktiver Provider;
- In-Memory Provider beziehungsweise Test Doubles für automatisierte Tests;
- reproduzierbare Datenbankmigrationen;
- lokal ausführbarer und containerisierbarer Build.

#### Persona

- Persona anlegen;
- Persona bearbeiten, indem eine neue immutable Persona-Version entsteht;
- Persona archivieren;
- Persona-Typ definieren;
- Grundidentität und Hintergrund pflegen;
- Communication Profile pflegen;
- Psychological Profile pflegen;
- Goals, Beliefs, Interessen und Expertise pflegen;
- Persona-Versionen nachvollziehen;
- drei stark unterscheidbare Persona Fixtures für Evaluation.

#### Psychological und Behavioral Model

- strukturierte `PsychologicalProfile`-Werte;
- validierter `TraitValue`;
- initiales versioniertes Trait Definition Set;
- fünf semantische Ausprägungsbänder;
- deterministischer, LLM-freier Behavioral Translator;
- strukturierter Behavioral Profile Output;
- versionierter Prompt Renderer;
- begrenzte algorithmische Verwendung ausgewählter Traits;
- Unit Tests für Translation und Rendering.

#### Relationships

- gerichtete Relationship Baseline zwischen Personas;
- Affinity, Trust, Respect und Conflict beziehungsweise die später verbindlich festgelegten Dimensionen;
- natürlichsprachlicher Beziehungskontext;
- Initialisierung von Session Relationship State;
- klare Trennung von globaler Baseline und Session State;
- keine automatische Rückübertragung von Sessionveränderungen.

#### Scenario

- Scenario anlegen;
- Scenario bearbeiten, indem eine neue immutable Scenario-Version entsteht;
- Ausgangssituation definieren;
- optionales Thema definieren;
- Scenario archivieren;
- Versionen historisch nachvollziehen.

#### Interaction Mode

- allgemeine Interaction-Mode-Abstraktion;
- versionierbare Definition und Konfiguration;
- mode-spezifischer State;
- mode-spezifische Allowed Actions;
- mode-spezifische Validierung und Completion Conditions;
- Registry beziehungsweise explizite Auflösung verfügbarer Modi;
- genau eine produktive Implementierung: `FreeSocietyInteractionMode`;
- ein Test Mode oder Test Double zum Nachweis der Austauschbarkeit, ohne zweite Produktfunktion.

#### Free Society

- deterministische Eligibility;
- keine Round-Robin-Pflicht;
- Behavioral Activation Policy;
- starker Direct Address Boost;
- grundlegende Event Relevance;
- Relationship Relevance;
- Recency Penalty;
- begrenzte algorithmische Nutzung von Talkativeness, Curiosity, Dominance, Impulsiveness und Patience;
- seedbare Weighted Random Selection;
- konfigurierbarer Candidate Threshold;
- `DoNothing`;
- Idle Handling;
- kein sofortiges Re-Rolling bis zwingend jemand spricht;
- Selection Trace;
- Schutz vor Monopolisierung durch aufeinanderfolgende Turns.

Die konkrete Scoreformel ist eine zu evaluierende Policy-Hypothese und keine wissenschaftliche Aussage.

#### Session

- Session in `DRAFT` anlegen;
- konkrete Persona-Versionen hinzufügen;
- konkrete Scenario-Version referenzieren;
- Interaction Mode auswählen;
- Runtime- und Tokenlimits konfigurieren;
- Startvoraussetzungen validieren;
- Session starten;
- Session pausieren;
- Session fortsetzen;
- Session regulär abschließen;
- Session abbrechen;
- pausierte Session später laden und fortsetzen;
- genau eine autonom aktive Session;
- beliebig viele persistierte Sessions;
- vollständige autoritative Session-Historie;
- keine automatische Mutation globaler Persona- oder Relationship-Daten.

#### Participant Actions und LLM

- logisch getrennte WHO-, WHAT- und HOW-Verantwortung;
- für den MVP ein strukturierter One-Call-Decision-and-Generation-Request;
- kleines gemeinsames Action Set;
- `Speak`, `React`, `Enter`, `Leave` und `DoNothing`, soweit für den ersten FreeSociety-Ablauf erforderlich;
- optionales Target und Intent;
- Structured Output;
- syntaktische und fachliche Action Validation;
- LLM-Output ist niemals direkt ein Domain Event;
- Domain Mapping von validierter Action auf Events;
- begrenzter Korrekturversuch bei ungültigem strukturiertem Output;
- keine Persistierung von Chain-of-Thought.

#### Context Builder

- vollständig rekonstruierbarer Kontext;
- stabile getrennte Context Blocks;
- deterministische Serialisierung;
- stabiles Event Rendering;
- Stable Global Context;
- Stable Scenario Context;
- Stable Persona Context;
- Stable Relationship Context;
- einfache erste Darstellung historischen Kontexts;
- append-only Recent Event Window;
- Dynamic Participant State;
- Current Trigger;
- Tokenzählung;
- Context Budget;
- Generation Reserve;
- konfigurierbares Hard Limit unterhalb des vLLM-Maximums;
- Debug View für Context Blocks und finalen Modellrequest;
- keine fachliche Abhängigkeit von Prefix Cache Hits.

#### Runtime

- diskrete Runtime-Impulse;
- genau ein sinnvoller Orchestrierungsschritt pro Impuls;
- genau ein globaler Inference Slot;
- keine spekulativen Turns;
- kein Lock und keine Datenbanktransaktion über einen vLLM-Aufruf;
- persistierter oder zuverlässig rekonstruierbarer operationaler Turn State;
- Expected Session Version pro Turn;
- stale-result protection;
- bounded Runtime Queue;
- höchstens ein geplanter autonomer Folgeschritt;
- Cancellation bei Pause oder Abort;
- harte Inference- und Generation-Timeouts;
- begrenzte und klassifizierte Retries;
- fehlgeschlagene Turns blockieren weder UI noch administrative Commands;
- Human- und Admin-Priorität;
- Manual Step Mode;
- kontrollierter Auto Mode.

#### Serverautonomie und UI-Unabhängigkeit

- Session Runtime läuft ohne verbundenes UI weiter;
- UI/API Requests führen keine Simulationsschleife im Requestpfad aus;
- Browser-Verbindungen besitzen keine Session;
- mehrere Tabs dürfen denselben Zustand betrachten;
- Commands werden serverseitig validiert;
- konkurrierende Commands verwenden Expected Version und Optimistic Concurrency;
- SSE-Verbindungen können getrennt geschlossen und neu aufgebaut werden;
- UI kann den aktuellen Zustand jederzeit neu laden.

#### Zugriffsschutz

- genau eine konfigurierte Benutzeridentität;
- Login mit sicheren serverseitigen Credentials;
- sicher gehashte Passwortspeicherung beziehungsweise sicherer externer Secret-Bezug;
- sichere Cookie-basierte Browser-Session oder gleichwertiger Mechanismus;
- CSRF-Schutz für verändernde Browser-Requests;
- HTTPS am regulären Ingress;
- keine anonymen Fach-, Admin- oder SSE-Endpunkte;
- kein Multi-Tenant- oder Rollenmodell im MVP.

#### UI

- moderne browserbasierte Benutzeroberfläche;
- Login;
- Persona-Verwaltung;
- Scenario-Verwaltung;
- Relationship-Verwaltung;
- Session-Konfiguration;
- Session Player;
- Live Event Feed;
- visuelle Unterscheidung von Personas;
- Human Message;
- Scenario Event Injection;
- Start, Pause, Resume, Step, Complete und Abort entsprechend den Domain Rules;
- Anzeige gespeicherter Sessions;
- Laden und Fortsetzen pausierter Sessions;
- Runtime- und Dependency-Status;
- Selection Trace;
- Context Debug View;
- verständliche Concurrency- und Domain-Fehler;
- UI enthält keine fachliche Geschäftslogik.

Die konkrete Frontend-Technologie und detaillierte Screen-Struktur werden im Frontend-Arbeitspaket entschieden.

#### Live Updates

- SSE als produktiver Live-Transport;
- Session-bezogene Streams;
- Reconnect und Replay ab bekanntem Event beziehungsweise Cursor;
- persistierte Events werden erst nach erfolgreichem Commit publiziert;
- unfertige Streaming-Tokens bleiben optionaler temporärer UI-State;
- Projection oder Publication Failure beschädigt keinen Domain Commit.

#### Recovery und Betrieb

- getrennte Liveness-, Readiness- und Dependency-Health-Zustände;
- vLLM-Ausfall macht die Anwendung degraded, aber nicht unerreichbar;
- PostgreSQL-Ausfall verhindert schreibende fachliche Operationen;
- Background-Fehler werden pro Runtime-Impuls isoliert;
- Startup erkennt zuvor aktive Sessions;
- kein ungeprüftes automatisches Resume nach Prozessabbruch;
- `RecoveryRequired` als operationaler Zustand;
- Benutzer kann fortsetzen, pausieren oder abbrechen;
- Admin- und Recovery-Ansicht;
- Runtime Reset verändert keine Domain Events;
- Projection Recovery über Prozessneustarts;
- vollständiger Read-Model-Rebuild;
- strukturierte Logs und Correlation IDs;
- Health Checks für ASP.NET Core, PostgreSQL, vLLM und Projection State.

#### Evaluation und Tests

- Architekturtests für Projektreferenzen;
- Domain Given-When-Then-Tests;
- Event-Apply-Tests;
- Rehydrationstests;
- Snapshot-Äquivalenztests, sobald Snapshots aktiv sind;
- Projection-, Idempotency- und Recovery-Tests;
- Application- und Orchestrierungstests;
- Context-Determinismus-Tests;
- Restart-Test für Anwendung;
- Restart- beziehungsweise Cache-Loss-Test für vLLM;
- Persona Identity Test;
- Persona Classification Test;
- Context Persistence Test;
- Relationship Test;
- erste Persona Collapse Detection;
- standardisierte Drei-Persona-FreeSociety-Session;
- Prefix-Cache-Benchmarks einschließlich `A-B-C-A`;
- Messung von Prompt Tokens, Cached Tokens, Prefill, TTFT und Gesamtdauer.

### 7.2 SHOULD

`SHOULD` bezeichnet Funktionen, die für einen guten MVP erwünscht sind, aber bei begründetem Zeit- oder Qualitätsrisiko auf die unmittelbar folgende Iteration verschoben werden dürfen.

- Persona Preview vor Speicherung einer Version;
- Side-by-Side-Vergleich zweier Persona-Versionen;
- exportierbares Behavioral Profile;
- einfacher Export einer Session als JSON oder Markdown;
- einfache Visualisierung von Relationship Baselines;
- UI-Filter für Eventtypen und Participants;
- Anzeige von Prompt- und Generation-Metriken pro Turn;
- manuell auslösbarer Read-Model-Rebuild in der Admin-Ansicht;
- optionale temporäre Token-Streaming-Anzeige;
- PWA-Installierbarkeit;
- einfache Sicherungs- und Wiederherstellungsanleitung für PostgreSQL.

SHOULD-Elemente dürfen nicht stillschweigend die Abnahme zentraler MUST-Invariants ersetzen.

### 7.3 LATER

`LATER` bezeichnet fachlich vorgesehene Erweiterungen, die im MVP nicht implementiert werden.

- zweite produktive Interaction-Mode-Implementierung;
- Committee;
- TalkShow;
- Debate;
- persistentes Persona-Memory über Sessions;
- Relationship Consolidation in globale Baselines;
- automatische Relationship Evaluation;
- automatische Mood Evaluation;
- Semantic Memory;
- Embeddings und Vector Retrieval;
- Persona- oder Scenario-spezifisches RAG;
- mehrere Orte und selektive Wahrnehmung;
- persistente Welten;
- World Controller;
- zeitgesteuerte autonome Scenario Events;
- Session Branching;
- Regeneration ab historischem Punkt;
- mehrere Modelle oder Inference-Endpunkte gleichzeitig;
- parallele Inference;
- mehrere autonom aktive Sessions;
- Multi-Session Scheduling und Fairness;
- Multi-User- und Tenant-Modell;
- Rollen- und Berechtigungsverwaltung;
- gemeinsames Bearbeiten oder Beobachten durch verschiedene Benutzer;
- native Desktop-Verpackung;
- SQLite als optionaler lokaler Produktprovider;
- komplexe modellabhängige Sampling Policies;
- LLM-basierte Actor Selection;
- embedding-basierte Event Relevance.

### 7.4 NOT NOW

`NOT NOW` bezeichnet bewusst unerwünschte Implementierungen. Sie dürfen nicht vorsorglich eingebaut werden.

- Microservices innerhalb des Playground Backends;
- externer Message Broker;
- Actor Framework;
- Prozess, Thread, Worker oder Pod pro Persona;
- direkte Agent-zu-Agent-Aufrufe;
- unkontrollierte rekursive Agentenketten;
- autonome Tool-Erzeugung;
- Internet-, Shell-, E-Mail- oder sonstige externe Tools für Personas;
- Application-seitiger KV-Cache-Manager;
- KV-Cache-Transfer zum Application Worker;
- permanenter KV-Cache pro Persona;
- fachliche Abhängigkeit von vLLM Cache State;
- Event Sourcing für technische Records, Read Models, Projection Checkpoints, Idempotency- oder Runtime-Zustand;
- beliebiger benutzerdefinierter Code für Interaction Modes;
- universelle Workflow- oder Policy-Sprache;
- globale statische Sessionobjekte;
- clientseitige Domain- oder Runtime-Steuerung;
- automatisches Endlos-Retry bei Inference- oder Projection-Fehlern;
- ungeprüftes Auto-Resume nach einem Prozessabbruch.

---

## 8. First Vertical Slice

Der First Vertical Slice ist kein separates Wegwerfprodukt. Er ist der erste ausführbare Zustand derselben Anwendung.

Er soll mindestens folgenden Ablauf durchgängig beweisen:

```text
vordefinierte Persona-Versionen
        |
vordefiniertes Scenario
        |
Session mit FreeSociety anlegen
        |
Session starten oder einen Step auslösen
        |
Participant auswählen
        |
Context Blocks erzeugen
        |
Inference über Fake Client, danach echten vLLM Client
        |
ParticipantAction validieren
        |
Session Domain Event appendieren
        |
Read Model projizieren
        |
Event und Runtime State im UI anzeigen
```

Fixtures und reduzierte Admin-Oberflächen sind für den ersten Slice zulässig. Die verwendeten Aggregate, Event Streams, Ports, Projections und Runtime-Grenzen müssen jedoch dem dauerhaften Zielmodell entsprechen.

---

## 9. MVP-Abnahmeszenario

Das zentrale Abnahmeszenario verwendet:

```text
3 deutlich unterschiedliche Personas
1 versioniertes Scenario
FreeSocietyInteractionMode
1 aktive Session
1 menschlicher Beobachter und Controller
lokaler konfigurierter vLLM-Endpunkt
PostgreSQL
Web UI
```

Die Abnahme prüft mindestens:

1. Personas bleiben über mehrere Turns unterscheidbar.
2. Sprecherwahl ist nicht starr und bleibt nachvollziehbar.
3. Direkte Adressierung und Relationships beeinflussen Verhalten.
4. `DoNothing` und Idle sind möglich.
5. Human Input und Scenario Events werden berücksichtigt.
6. Pause, Resume und Step funktionieren.
7. Das Schließen des Browsers stoppt die Session nicht.
8. Ein zweiter Tab zeigt denselben zentralen Zustand.
9. Widersprüchliche Tab-Commands werden sicher behandelt.
10. vLLM-Ausfall blockiert weder UI noch Abort oder Recovery.
11. Ein Prozessneustart verliert keinen Domain State.
12. Eine zuvor aktive Session wird nicht ungeprüft automatisch fortgesetzt.
13. Read Models können verworfen und neu aufgebaut werden.
14. Context und Verhalten bleiben nach KV-Cache-Verlust fachlich korrekt.
15. Runtime- und Tokenlimits verhindern unkontrollierte Ausführung.

---

## 10. Definition of Done

Der MVP ist abgeschlossen, wenn:

- sämtliche nicht ausdrücklich verschobenen MUST-Anforderungen implementiert und getestet sind;
- der First Vertical Slice in die vollständige MVP-Anwendung weiterentwickelt wurde;
- das zentrale Abnahmeszenario erfolgreich durchlaufen wurde;
- keine fachlich relevante Information ausschließlich in UI, Runtime Queue oder KV-Cache lebt;
- die Anwendung ohne verbundenes UI weiterarbeiten kann;
- mehrere Tabs konsistent denselben serverseitigen Zustand darstellen;
- ein fehlerhafter Turn nicht den Zugang zur Anwendung blockiert;
- PostgreSQL, Event Store, Projections und Recovery funktionsfähig sind;
- FreeSociety über die allgemeine Interaction-Mode-Abstraktion implementiert ist;
- keine zweite produktive Mode-Implementierung erforderlich ist;
- Architektur-, Domain-, Projection-, Runtime- und Behavioral-Tests vorhanden sind;
- die Anwendung lokal und im k3s-Zielbetrieb reproduzierbar gestartet werden kann;
- alle bewusst verschobenen Funktionen weiterhin als `LATER` oder `NOT NOW` dokumentiert sind.

---

## 11. Änderung des MVP-Scopes

Änderungen an diesem Scope erfolgen ausdrücklich und nachvollziehbar.

Jede Änderung muss angeben:

- betroffene Kategorie;
- fachliche oder technische Begründung;
- Auswirkung auf Implementierungsplan und Abnahmekriterien;
- neu entstehende Risiken;
- ob die Änderung den MVP erweitert, reduziert oder lediglich präzisiert.

Ein Feature wird nicht allein deshalb in den MVP aufgenommen, weil seine spätere Umsetzung bereits vorstellbar ist.

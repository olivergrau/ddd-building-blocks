# Persona Simulation Playground

## Architecture Decisions

**Status:** Verbindliches Architekturdokument  
**Version:** 1.1  
**Datum:** 2026-08-15  
**Geltungsbereich:** Technische Zielarchitektur, Domain- und Persistenzprinzipien, Dokumenthierarchie und ersetzte Annahmen

---

## 1. Zweck und Verbindlichkeit

Dieses Dokument konsolidiert die aktuell verbindlichen Architekturentscheidungen des Persona Simulation Playground.

Es löst Widersprüche zwischen früheren Konzeptionsständen auf. Detaildokumente bleiben als fachliche und technische Vertiefung gültig, soweit sie diesem Dokument und späteren ausdrücklich beschlossenen Entscheidungen nicht widersprechen.

Neue Architekturentscheidungen werden künftig nachvollziehbar dokumentiert. Eine spätere Entscheidung ersetzt eine frühere nur dann, wenn dies ausdrücklich kenntlich gemacht wird.

---

## 2. Architekturüberblick

Die Anwendung wird als domain-zentrierter modularer Monolith in einem .NET-Prozessraum implementiert.

```text
Web UI
   |
   | REST / SSE
   v
ASP.NET Core Application
   |
   +-- API / Composition Root
   +-- Application Services
   +-- Session Orchestrator
   +-- Interaction Modes
   +-- Context Builder
   +-- Background Runtime
   +-- Domain Model
   +-- Infrastructure Adapters
   |
   +--> Database / Event Store / Read Store
   |
   +--> vLLM on DGX Spark
```

Innerhalb der Anwendung werden fachliche und technische Grenzen durch getrennte Assemblies, Abhängigkeitsregeln und Tests geschützt. Dafür werden keine internen Prozess- oder Netzwerkgrenzen benötigt.

---

## 3. Verbindliche Entscheidungen im Überblick

| ID | Entscheidung | Status |
|---|---|---|
| AD-001 | C# und ASP.NET Core auf Linux | Accepted |
| AD-002 | Domain-centric modular monolith | Accepted |
| AD-003 | Ein .NET-Prozessraum für die Anwendung | Accepted |
| AD-004 | Getrennte Assemblies und inward dependencies | Accepted |
| AD-005 | Direkte Nutzung von DDD.BuildingBlocks im Domain Model | Accepted |
| AD-006 | Review und Modernisierung von DDD.BuildingBlocks vor Projekteinsatz | Accepted |
| AD-007 | Event Sourcing für alle fachlichen Aggregate | Accepted, revidiert 2026-08-16 |
| AD-008 | CQRS mit getrennter Write- und Read-Side | Accepted |
| AD-009 | Aggregate Event Streams sind autoritative fachliche Persistenz | Accepted, revidiert 2026-08-16 |
| AD-010 | Read Models sind rekonstruierbar und wegwerfbar | Accepted |
| AD-011 | Snapshots sind reine Performanceoptimierung | Accepted |
| AD-012 | Persist-before-publish | Accepted |
| AD-013 | Application Ports werden in Application definiert | Accepted |
| AD-014 | Domain besitzt keine infrastrukturspezifischen Ports | Accepted |
| AD-015 | In-Process Runtime Coordination | Accepted |
| AD-016 | Global serielle Persona-Inference | Accepted |
| AD-017 | vLLM ist externe Inference-Infrastruktur | Accepted |
| AD-018 | Rekonstruierbarer Context Builder und flüchtiger KV-Cache | Accepted |
| AD-019 | REST für Commands und SSE für Live-Updates | Accepted |
| AD-020 | Keine unnötige verteilte Infrastruktur im MVP | Accepted |
| AD-021 | Persönliche, zentral gehostete Single-User-Anwendung | Accepted |
| AD-022 | Serverseitig autoritative Runtime und unabhängiges Web-UI | Accepted |
| AD-023 | Genau eine autonom aktive Session | Accepted |

---

## 4. AD-001: Backend-Technologie

Das Backend wird implementiert mit:

```text
Language: C#
Framework: ASP.NET Core
Runtime: .NET auf Linux
Deployment: Linux Container in k3s
```

Python ist keine Backend-Voraussetzung. vLLM wird über seine OpenAI-kompatible HTTP-Schnittstelle angesprochen.

Python kann später für isolierte Spezialkomponenten verwendet werden, wenn dafür ein konkreter Nutzen entsteht. Es wird nicht vorsorglich Teil der Kernarchitektur.

---

## 5. AD-002 und AD-003: Modularer Monolith in einem Prozessraum

Das Backend ist ein:

> Domain-centric modular monolith

Die Anwendung läuft zunächst in genau einem ASP.NET-Core-Prozess beziehungsweise Backend-Pod.

Mehrere Assemblies bedeuten keine Microservices.

Innerhalb des Prozesses dürfen parallel stattfinden:

- REST Requests;
- SSE Streams;
- Datenbankzugriffe;
- Background Processing;
- Runtime Coordination;
- ein laufender vLLM-Request;
- Logging und Health Checks.

Zwischen internen Modulen ist keine Process-to-Process-Kommunikation erforderlich.

Externe Kommunikation existiert nur zu tatsächlicher Infrastruktur, beispielsweise:

- Datenbank;
- vLLM-Endpunkt;
- Browser;
- später gegebenenfalls externe Storage- oder Observability-Systeme.

### Konsequenzen

Nicht Teil des MVP sind:

- interne Microservices;
- interner HTTP-Verkehr zwischen Fachmodulen;
- externer Message Broker;
- verteilte Transaktionen;
- Actor Runtime;
- Prozess oder Pod pro Persona.

---

## 6. AD-004: Solution- und Abhängigkeitsstruktur

Das initiale Zielbild lautet:

```text
PersonaPlayground.sln

src/
  PersonaPlayground.Api
  PersonaPlayground.Application
  PersonaPlayground.Domain
  PersonaPlayground.Infrastructure

tests/
  PersonaPlayground.Domain.Tests
  PersonaPlayground.Application.Tests
  PersonaPlayground.IntegrationTests
```

Zusätzliche Projekte werden nur bei einer konkret begründeten fachlichen oder technischen Grenze ergänzt.

Die grundsätzliche Dependency Direction lautet:

```text
API ----------> Application ----------> Domain
 |                    ^                    ^
 |                    |                    |
 +------------> Infrastructure -----------+
```

### Domain

Enthält:

- Aggregate Roots;
- Entities;
- Value Objects;
- Domain Events;
- Domain Services, soweit fachlich erforderlich;
- Invariants und State Machines;
- fachliche Actions und Resultate.

Domain kennt nicht:

- ASP.NET Core;
- EF Core;
- HTTP;
- SQLite oder PostgreSQL;
- vLLM oder das OpenAI-Protokoll;
- Kubernetes;
- SSE;
- technische Message Broker.

### Application

Enthält:

- Use Cases;
- Application Services;
- Session Orchestrator;
- Interaction-Mode-Koordination;
- Participant Selection;
- Context Builder;
- Turn Coordination;
- benötigte Ports;
- Runtime Coordination.

### Infrastructure

Enthält konkrete Adapter für:

- Event Store sowie konventionelle Persistenz für technische und abgeleitete Daten;
- Read Store und Projections;
- Snapshots;
- EF Core und Datenbankprovider;
- vLLM HTTP Client;
- technische Live-Publication;
- technische Observability.

### API

Ist:

- Host;
- Composition Root;
- Transport-Fassade;
- Konfigurations- und DI-Einstiegspunkt;
- Runtime Host für Background Services.

Das API-Projekt enthält keine Geschäftslogik und keinen autoritativen fachlichen Zustand.

---

## 7. AD-005: Direkte Nutzung von DDD.BuildingBlocks

Das bestehende Framework `DDD.BuildingBlocks` wird als taktische Grundlage verwendet.

Die Playground-Domain darf fachneutrale Typen und Mechanismen des Frameworks direkt verwenden, beispielsweise für:

- Aggregate Roots;
- event-sourced Aggregate Roots;
- Entities;
- Value Objects;
- Domain Events;
- Snapshotting;
- Repositories beziehungsweise Aggregate Stores, entsprechend der tatsächlichen Frameworkstruktur;
- Command- und Event-Sourcing-Mechanismen.

Damit gilt:

> Der Domain-Kern ist unabhängig von Infrastruktur- und Application-Frameworks, aber nicht künstlich von den eigenen fachneutralen taktischen DDD.BuildingBlocks entkoppelt.

Es wird keine parallele zweite Sammlung taktischer Basistypen nur zur scheinbaren Framework-Unabhängigkeit entwickelt.

DDD.BuildingBlocks darf keine Persona- oder Simulationsfachlichkeit enthalten.

---

## 8. AD-006: Technisches Review von DDD.BuildingBlocks

Vor Verwendung im Playground wird DDD.BuildingBlocks in einer eigenen Vorstufe überprüft und bei Bedarf modernisiert.

Mindestens zu prüfen sind:

- aktuelle .NET-Kompatibilität;
- Build- und Teststatus;
- Nullable Reference Types;
- Async- und Cancellation-Patterns;
- Dependency Injection;
- Event Store Abstraktionen;
- Optimistic Concurrency;
- Snapshotting;
- Projections und Recovery;
- EF-Core- und Provider-Kompatibilität;
- Linux- und Container-Kompatibilität;
- obsolete Dependencies;
- Abhängigkeitsgrenzen.

Leitprinzip:

> Modernisieren und absichern, nicht unnötig neu erfinden.

Der Review darf konkrete Änderungen am Framework ergeben. Er darf jedoch nicht ungeprüft Fachlichkeit des Playground in das Framework verlagern.

---

## 9. AD-007: Event Sourcing für alle fachlichen Aggregate

Alle echten fachlichen Aggregate des Playground verwenden Event Sourcing. Damit folgt das Write Model einem einheitlichen Entscheidungs-, Concurrency- und Persistenzmodell und bewahrt den Fokus von `DDD.BuildingBlocks`.

### Event-sourced Domain Aggregates

```text
Session Aggregate
```

einschließlich:

- Session Lifecycle;
- Participants;
- Participant State;
- Session Relationship State;
- Interaction Mode State;
- fachlichem Simulationszustand.

### Weitere event-sourced Aggregate

```text
Persona
PersonaVersion
PersonaRelationship Baseline
Scenario
ScenarioVersion
InteractionModeDefinition
```

Technische Records, Read Models, Projection Checkpoints, Idempotency- und Runtime-Zustand sind keine Domain Aggregates und werden konventionell persistiert. `InteractionModeDefinition` ist nur dann ein event-sourced Aggregate, wenn sie tatsächlich nutzerverwaltet und persistent ist; fest ausgelieferte Definitionen bleiben Code oder Konfiguration.

Eine spätere Änderung der Persistenzstrategie eines Aggregates benötigt eine konkrete Begründung und eine neue Architekturentscheidung.

---

## 10. AD-008: CQRS

CQRS ist ein verbindliches Architekturprinzip.

Write Side und Read Side werden bewusst getrennt.

### Write Side

Die Write Side arbeitet mit:

- Commands;
- Application Services;
- Domain Aggregates;
- Domain Rules;
- autoritativer Aggregate-Persistenz;
- Optimistic Concurrency;
- Domain Events;
- Snapshots für event-sourced Aggregate.

### Read Side

Die Read Side arbeitet mit zweckgebundenen Read Models und Queries.

Read Models dürfen bewusst anders strukturiert sein als Aggregate. Es besteht keine Anforderung:

```text
Read Model == Domain Model
```

Die konkrete physische Trennung von Write- und Read-Datenbank ist für CQRS nicht erforderlich. Im MVP dürfen beide Seiten denselben Datenbankserver oder dieselbe Datenbanktechnologie verwenden, solange ihre Modelle und Verantwortungen getrennt bleiben.

CQRS impliziert keine verteilte Architektur.

---

## 11. AD-009: Source-of-Truth-Regeln

Aggregate sind die fachlichen Konsistenz- und Entscheidungsgrenzen.

Für alle fachlichen Aggregate ist der jeweilige Event Stream die autoritative persistente Wahrheit.

| Kategorie | Autoritative Source of Truth |
|---|---|
| Fachliches Aggregate | Aggregate Event Stream |
| Read Model | Nicht autoritativ |
| Projection | Nicht autoritativ |
| Snapshot | Nicht autoritativ |
| In-Memory Aggregate Instance | Nicht dauerhaft autoritativ |
| Application Runtime State | Nicht autoritativ |
| Runtime Channel | Nicht autoritativ |
| SSE Stream | Nicht autoritativ |
| vLLM Request State | Nicht autoritativ |
| vLLM KV Cache | Nicht autoritativ |

Für jedes fachliche Aggregate gilt; für `Session` insbesondere:

> Der Session Event Stream ist die einzige autoritative persistente Wahrheit über den Session Domain State.

Es wird keine zweite autoritative Tabelle mit dem aktuellen vollständigen Sessionzustand geführt.

---

## 12. AD-010: Read-Model-Eigenschaften

Alle aus event-sourced Aggregaten abgeleiteten Read Models erfüllen folgende Bedingungen:

- sie sind nicht autoritativ;
- sie sind aus ihren Quell-Eventstreams rekonstruierbar;
- sie sind vollständig wegwerfbar;
- sie können vollständig neu aufgebaut werden;
- Projection Apply ist idempotent;
- Projection-Fortschritt wird nachvollziehbar gespeichert;
- fehlende Events können inkrementell nachprojiziert werden;
- Projection Recovery funktioniert über Prozessneustarts hinweg;
- Projection Failure macht einen erfolgreichen Domain Commit nicht rückwirkend ungültig.

Eventual Consistency zwischen Write- und Read-Side wird akzeptiert.

Da die Anwendung zunächst in einem Prozessraum läuft, soll die normale Verzögerung gering sein. Korrektheit darf dennoch nicht von synchroner Projektion abhängen.

### Projektionsreihenfolge

```text
Domain Operation
      |
      v
authoritative persistence commit
      |
      v
projection dispatch
      |
      v
read model update
      |
      v
live publication
```

---

## 13. AD-011: Snapshots

Snapshots beschleunigen ausschließlich die Rehydration event-sourced Aggregates.

Sie sind:

- nicht autoritativ;
- verwerfbar;
- aus Event Streams rekonstruierbar;
- unabhängig von Projection Checkpoints;
- versionierbar beziehungsweise bei Inkompatibilität verwerfbar.

Die initiale Snapshot Policy soll einfach und konfigurierbar sein. Eine adaptive Optimierung erfolgt erst aufgrund von Messungen.

---

## 14. AD-012: Persist-before-publish

Ein fachliches Ereignis gilt erst nach erfolgreicher autoritativer Persistierung als eingetreten.

Verbindliche Reihenfolge:

```text
Domain Result
    |
    v
Persist / Event Append
    |
    v
Projection
    |
    v
Live Publication
```

SSE, Runtime Channels und andere Transportmechanismen dürfen nie vor erfolgreichem Domain Commit zur Source of Truth werden.

Ein Fehler bei Projection oder Publication darf den bereits erfolgreichen Domain Commit nicht ungeschehen machen. Er wird über Recovery behandelt.

---

## 15. AD-013 und AD-014: Ports und Dependency Inversion

Wenn Application-Funktionalität externe Fähigkeiten benötigt, definiert Application die dafür erforderlichen Ports.

Beispiele:

```text
IInferenceClient
IClock
IRandomSource
ILiveEventPublisher
ITokenCounter
```

Die endgültigen Namen und Verträge werden im Application-Port-Dokument festgelegt.

Infrastructure implementiert diese Ports.

Der Domain-Kern definiert keine Interfaces, die konkrete Infrastruktur abstrahieren, beispielsweise:

```text
IDatabase
IPostgresRepository
IVllmClient
IHttpClient
IMessageBroker
```

Die direkte Nutzung fachneutraler taktischer DDD.BuildingBlocks bleibt davon unberührt.

Ob ein generisches Aggregate-Store- oder Repository-Contract aus DDD.BuildingBlocks in Domain oder Application liegt, wird anhand der tatsächlichen Frameworkstruktur im Framework-Review geprüft. Die Playground-Domain definiert jedenfalls keine datenbankspezifischen Repositories.

---

## 16. AD-015: In-Process Runtime Coordination

Runtime Coordination erfolgt innerhalb des ASP.NET-Core-Prozesses.

Voraussichtliche technische Mittel:

- `System.Threading.Channels`;
- wenige ASP.NET Core `BackgroundService`s;
- `async`/`await`;
- Cancellation Tokens;
- persistierter fachlicher Zustand als Recovery-Grundlage.

Channels sind transient und keine Persistenz.

Ein Runtime-Impuls verarbeitet genau einen nachvollziehbaren Orchestrierungsschritt und kann anschließend einen weiteren Impuls planen.

Nicht bevorzugt ist eine unkontrollierte rekursive oder endlose Agentenschleife.

Runtime-State darf verloren gehen, ohne fachlichen Zustand zu verlieren. Nach einem Prozessneustart muss notwendige Arbeit aus autoritativer Persistenz und operationalen Recovery-Daten wieder erkannt werden können.

---

## 17. AD-016: Serielle Persona-Inference

Persona-Inference erfolgt im MVP global seriell.

Es generiert maximal eine Persona gleichzeitig.

Das beschränkt nicht andere parallele I/O-Aktivitäten der Anwendung.

Gründe:

- vorhandene Inference-Hardware;
- kontrollierbare Ressourcenverwendung;
- einfachere Sequenzialität;
- konsistente Turn-Verarbeitung;
- klare Human-Interruptibility;
- geringere Runtime-Komplexität.

Es werden keine spekulativen Turns erzeugt.

Vor Commit eines Inference-Ergebnisses werden Session-State und erwartete Aggregate-Version erneut geprüft. Stale Results werden standardmäßig verworfen und nicht auf einen neueren State umgebogen.

---

## 18. AD-017: vLLM als Infrastructure Boundary

vLLM läuft auf dem DGX Spark und wird als externe Inference-Infrastruktur behandelt.

Die Application kennt eine abstrakte Inference-Fähigkeit. Infrastructure implementiert den konkreten vLLM-Client über die OpenAI-kompatible HTTP-Schnittstelle.

Domain kennt keine:

- Modellnamen;
- vLLM-Konfiguration;
- HTTP-Verträge;
- Token Streaming Details;
- KV-Cache-Mechanismen.

Der DGX Spark führt Inference aus, aber keine Playground-Anwendungslogik.

---

## 19. AD-018: Kontext und KV-Cache

Der vollständige fachliche Zustand lebt auf dem Application- und Persistence-Tier.

Der Context Builder erzeugt für jeden Turn einen vollständigen, begrenzten und rekonstruierbaren Modellkontext.

Der vLLM KV-Cache ist ausschließlich eine flüchtige Performanceoptimierung.

Verbindliche Regeln:

- kein permanenter KV-Cache pro Persona;
- kein KV-Transfer zum Application Worker;
- kein eigener KV-Cache-Manager im Playground;
- keine funktionale Abhängigkeit von Cache Hits;
- Cache Misses dürfen nur Performance kosten;
- Automatic Prefix Caching wird opportunistisch genutzt;
- 256k Modellkontext ist Kapazität, nicht Standard-Promptgröße;
- Promptstabilität wird nicht auf Kosten fachlicher Korrektheit erzwungen.

Der Context Builder unterscheidet logisch:

```text
Stable Global Context
Stable Scenario Context
Stable Persona Context
Stable Relationship Context
Stable Historical Blocks
Append-only Recent Events
Dynamic Participant State
Current Trigger
```

Serialisierung und Eventrepräsentation sollen deterministisch sein, damit Rekonstruierbarkeit, Tests und Prefix Caching unterstützt werden.

---

## 20. AD-019: Externe Schnittstellen

Für den MVP gilt:

- REST für Human- und Admin-Commands sowie reguläre Queries;
- SSE für Live-Updates zum Browser;
- HTTP zur Kommunikation mit vLLM;
- Datenbankzugriff über Infrastructure Adapter.

Ein sprachlicher Persona-Beitrag wird erst als vollständiges fachliches Event persistiert.

Optionales tokenweises Streaming ist ein temporärer UI-Zustand und kein persistiertes Domain Event.

WebSockets oder andere bidirektionale Transportmechanismen werden nur eingeführt, wenn SSE plus REST die tatsächlichen Anforderungen nicht erfüllen.

---

## 21. AD-020: Keine unnötige Infrastruktur

Folgende Technologien werden nicht ohne konkreten Bedarf eingeführt:

```text
Microservices
RabbitMQ
Kafka
MassTransit
Orleans
Akka.NET
Redis
Hangfire
Kubernetes-Pod pro Persona
technische Mailbox pro Persona
verteilter Scheduler
externer KV-Cache
```

Austauschbarkeit entsteht zunächst durch:

- klare fachliche Grenzen;
- getrennte Assemblies;
- Application Ports;
- Infrastructure Adapter;
- Dependency Injection;
- Tests.

Austauschbarkeit erfordert keine vorsorgliche Verteilung.

---

## 22. Datenbankstrategie

PostgreSQL wird von Anfang an als produktiver Persistence Provider verwendet.

Für automatisierte Tests dürfen In-Memory Provider aus DDD.BuildingBlocks beziehungsweise geeignete Test Doubles verwendet werden.

Die konkrete Datenbank ist Infrastructure.

Domain und Application dürfen keine SQLite- oder PostgreSQL-spezifischen Annahmen enthalten.

Event Store, Snapshots, technische Persistenzmodelle und Read Models dürfen im MVP PostgreSQL gemeinsam verwenden. Ihre logischen Rollen und Source-of-Truth-Eigenschaften bleiben dennoch getrennt.

SQLite ist kein geplanter produktiver Zwischenschritt. Ein späterer SQLite-Provider bleibt technisch möglich, benötigt aber einen konkreten Anwendungsfall.

---

## 22.1 AD-021: Persönliche, zentral gehostete Single-User-Anwendung

Der Persona Simulation Playground ist im MVP eine persönliche, selbst gehostete Single-User-Anwendung. Der reguläre Betrieb erfolgt zentral im privaten Homelab und bevorzugt im vorhandenen k3s-Cluster.

```text
Browser
   |
   v
Persona Playground im k3s-Cluster
   |
   +--> PostgreSQL
   +--> konfigurierbarer vLLM-Endpunkt
```

Single User bedeutet:

- genau eine konfigurierte berechtigte Benutzeridentität;
- keine Multi-Tenancy;
- kein fachliches `User`- oder `Workspace`-Aggregate;
- keine Owner- oder Tenant-IDs in sämtlichen Aggregaten;
- keine parallelen autonomen Sessions verschiedener Benutzer;
- kein anonymer Zugriff auf UI oder API.

Single User bedeutet ausdrücklich nicht:

- nur einen Browser-Tab;
- nur eine HTTP-Verbindung;
- einen clientgebundenen Sessionzustand;
- einen globalen statischen `CurrentSession`-Zustand;
- fehlende Authentifizierung im Heimnetz.

Mehrere Browser-Tabs und Geräte des berechtigten Benutzers dürfen sich gleichzeitig anmelden und denselben serverseitigen Zustand beobachten oder steuern.

Authentifizierung schützt den Zugang zur Anwendung, ohne ein fachliches Mehrbenutzermodell einzuführen. Die konkrete Authentifizierung wird im Sicherheits- und Frontend-Design präzisiert.

Die Anwendung bleibt lokal über `dotnet run` entwickelbar. Das ändert nicht den zentral gehosteten regulären Betriebsmodus.

---

## 22.2 AD-022: Serverseitig autoritative Runtime und unabhängiges Web-UI

Der vollständige fachliche und operationale Anwendungszustand wird serverseitig verwaltet.

Das Web-UI ist ein unabhängiger, austauschbarer Beobachtungs- und Steuerungsclient.

Das UI:

- authentifiziert sich an der zentralen Anwendung;
- liest Read Models;
- abonniert serverseitige Live-Updates;
- visualisiert Domain-, Runtime- und Fehlerzustände;
- sendet Commands;
- besitzt eigene Navigations-, Formular- und Darstellungsflüsse;
- kann Beobachtung, Persona-Pflege, Scenario-Konfiguration, Session-Steuerung und Diagnose als getrennte User Flows anbieten.

Das UI ist nicht:

- Eigentümer einer Session;
- Lebenszyklusanker der Runtime;
- Source of Truth;
- zuständig für Domain Validation;
- zuständig für Turn Scheduling;
- zuständig für Persistenz oder Recovery.

Eine laufende Session läuft unabhängig von verbundenen UI-Clients weiter. Das Schließen des Browsers pausiert oder beendet die Simulation nicht.

Mehrere Tabs verhalten sich wie mehrere Ansichten auf dieselbe zentrale Anwendung. Konfligierende Commands werden serverseitig über Domain Rules, erwartete Aggregate-Versionen, Idempotency und Optimistic Concurrency behandelt.

Ein UI darf Buttons deaktivieren oder mögliche Aktionen vorfiltern. Die verbindliche fachliche Entscheidung erfolgt dennoch ausschließlich serverseitig.

Kein fachlich relevanter Zustand darf ausschließlich im UI existieren. Temporärer UI-State wie ein noch nicht abgesendetes Formular, lokale Filter oder die Darstellung unfertiger Streaming-Tokens darf clientseitig bleiben.

---

## 22.3 AD-023: Genau eine autonom aktive Session

Es können beliebig viele Sessions persistiert, betrachtet, pausiert und später fortgesetzt werden.

Zu einem Zeitpunkt wird jedoch genau eine Session autonom abgespielt.

Vor dem Aktivieren einer anderen Session muss die aktuelle Session pausiert, regulär abgeschlossen oder abgebrochen werden.

Multi-Session Runtime, konkurrierende Simulationen und Fairness Scheduling zwischen Sessions sind weder Bestandteil des MVP noch derzeit ein konkretes Produktziel.

Alle Sessionoperationen bleiben trotzdem explizit über `SessionId` adressiert. Die Single-Active-Session-Policy rechtfertigt keine versteckten globalen Domainzustände.

Die Web/API-Erreichbarkeit ist von der Gesundheit der aktiven Simulation unabhängig. Ein hängender oder fehlgeschlagener Turn darf weder UI noch administrative Commands blockieren.

---

## 23. Concurrency und Atomicity

Das Session Aggregate bildet die zentrale fachliche Concurrency-Grenze.

Für event-sourced Aggregate gilt Optimistic Concurrency über die erwartete Aggregate-Version.

Ein Turn merkt sich mindestens:

```text
SessionId
ParticipantId
ExpectedSessionVersion
TurnId
operationaler Status
```

Zwischen Inference-Beginn und Event Append kann sich die Session ändern. Bei Versionskonflikt ist das Resultat stale und wird standardmäßig verworfen.

Die autoritative Persistierung eines erfolgreichen Turns muss atomar und deduplizierbar erfolgen. Die genaue Transaktionsgrenze wird im Domain- und Application-Contract-Dokument festgelegt.

---

## 24. Recovery und Restart

Ein Anwendungs- oder vLLM-Neustart darf keinen fachlichen Zustand verlieren.

Verloren gehen dürfen:

- In-Memory Aggregate Instances;
- Runtime Channel Inhalte;
- SSE Connections;
- laufende HTTP Requests;
- vLLM KV-Cache;
- temporäre Token Streams.

Rekonstruierbar bleiben müssen:

- Aggregate State;
- Session Event History;
- Read Models;
- benötigte Context Inputs;
- Projection-Fortschritt;
- notwendige Recovery-Entscheidungen.

Ein technischer Fehler ist nicht automatisch ein fachlicher `ERROR`-Zustand der simulierten Welt.

---

## 25. Dokumenthierarchie

Die vorhandenen Dokumente werden wie folgt eingeordnet.

| Dokument | Rolle | Aktuelle Verbindlichkeit | Hinweise |
|---|---|---|---|
| `00-Project-Vision.md` | konsolidierte Produktvision | verbindlich | primäre Quelle für Zielbild und Anti-Ziele |
| `01-Architecture-Decisions.md` | konsolidierte Architekturentscheidungen | verbindlich | hat bei Architekturwidersprüchen Vorrang |
| `Social-Experiment-Platform.md` | fachlicher Kickoff und Detailquelle | ergänzend verbindlich | soweit kein Widerspruch zur Project Vision besteht |
| `Technisches Konzept.md` | Speicher-, Kontext- und KV-Architektur | ergänzend verbindlich | Cache- und Context-Prinzipien bleiben gültig |
| `Technischer Zusatz-für-KV-Cache-Handling.md` | Prefix-Cache-Präzisierung | ergänzend verbindlich | ergänzt das technische Konzept |
| `Erweitertes-Technisches Konzept.md` | modulare Ziel- und Runtime-Architektur | teilweise verbindlich | frühere Session-Persistenzannahme wurde ersetzt |
| `Datenmodell.md` | früher fachlicher Domain-Schnitt | historische Detailquelle | Aggregate und Begriffe teilweise gültig, Session-Persistenz ersetzt |
| `Psychological-Profile.md` | Behavioral- und Psychological-Konzept | ergänzend verbindlich | MVP-Umfang wird separat festgelegt |
| `DomainModel-EventSourcing-CQRS.md` | aktueller Domain- und Persistenzstand | verbindlich | ersetzt frühere Ablehnung von Event Sourcing für Session |
| `Orchestrierung und Interaction Modes.md` | Orchestrierungskonzept | ergänzend verbindlich | konkrete Policy-Kalibrierung bleibt Hypothese |
| `Weitere-Spec-Vorgehen.md` | historische Planungsnotiz | nicht normativ | dokumentierte Lücken wurden weitgehend bearbeitet |
| `Vorbereitungsplan-vor-Implementierung.md` | Arbeits- und Steuerungsplan | prozessual verbindlich | steuert weitere Spezifikationsarbeit |

### Auslegungsregel

Bei Widersprüchen gilt folgende Reihenfolge:

1. ausdrücklich bestätigte aktuelle Benutzerentscheidung;
2. `01-Architecture-Decisions.md`;
3. `00-Project-Vision.md` für Produkt- und Zielkonflikte;
4. spezialisiertes späteres Detaildokument;
5. allgemeines oder früheres Konzeptdokument;
6. historische Planungsnotiz.

Ein späteres Datum allein ersetzt keine Entscheidung. Die Ersetzung muss sachlich erkennbar oder ausdrücklich dokumentiert sein.

---

## 26. Supersession Matrix

| Frühere Annahme | Aktuelle Entscheidung | Status |
|---|---|---|
| Session besitzt separat gespeicherten Current State plus persistente Event History | Session Event Stream ist alleinige autoritative Persistenz des Session Domain State | ersetzt |
| Event Sourcing ist für den MVP nicht erforderlich | Event Sourcing gilt für alle fachlichen Aggregate | ersetzt |
| Persona, Scenario und Relationship Baseline besitzen zustandsbasierte Write Stores | Auch diese fachlichen Aggregate besitzen eigene Event Streams; ihre Read Models sind abgeleitet | ersetzt am 2026-08-16 |
| Aktueller Participant State kann autoritativ separat gespeichert werden | Participant State ist Teil des event-sourced Session Aggregates; Tabellen sind höchstens Read Models | ersetzt |
| Session Relationship State kann autoritativ separat gespeichert werden | Session Relationship State wird aus Session Events aufgebaut | ersetzt |
| CQRS ist optionale spätere Architektur | CQRS ist gesetzt | ersetzt |
| Read Models könnten gewöhnliche autoritative CRUD-Tabellen sein | Read Models sind abgeleitet, rekonstruierbar und wegwerfbar | ersetzt |
| Domain ist vollständig frei von allen Frameworktypen | Domain darf fachneutrale taktische DDD.BuildingBlocks direkt verwenden | präzisiert |
| Austauschbarkeit könnte durch separate Services erreicht werden | Austauschbarkeit wird zunächst durch Module, Ports und Adapter erreicht | präzisiert |
| Persona-Agenten könnten eigene Runtime-Einheiten sein | Personas sind fachliche Objekte, keine Prozesse, Pods oder Worker | verworfen |
| KV-Cache könnte Persona-State repräsentieren | KV-Cache ist ausschließlich flüchtige Inference-Optimierung | verworfen |

---

## 27. Verbindliche Architektur-Invariants

1. Der jeweilige Aggregate Event Stream ist Source of Truth für jeden fachlichen Aggregate State.
2. Konventionell persistierte technische und abgeleitete Daten sind keine Domain Aggregates und erben von keiner Aggregate-Root-Abstraktion.
3. Read Models sind nicht autoritativ, rekonstruierbar und wegwerfbar.
4. Snapshots sind reine Performanceoptimierungen.
5. Domain Event Apply ist deterministisch und side-effect-frei.
6. LLM-Outputs werden vor der Persistierung als fachliche Actions validiert.
7. Operational Events gehören nicht in den fachlichen Session Stream.
8. Ein Domain Commit erfolgt vor Projection und Live Publication.
9. Projection Failure darf einen erfolgreichen Domain Commit nicht rückgängig machen.
10. Runtime und Kontext sind aus persistiertem Zustand rekonstruierbar.
11. Domain kennt keine konkrete externe Infrastruktur.
12. DDD.BuildingBlocks darf als fachneutrale taktische Grundlage direkt verwendet werden.
13. Die Anwendung läuft im MVP in einem .NET-Prozessraum.
14. Maximal eine Persona generiert gleichzeitig.
15. Keine fachliche Information lebt ausschließlich im vLLM KV-Cache.
16. Sessionbezogene Veränderungen mutieren globale Persona- oder Relationship-Definitionen nicht automatisch.
17. Deterministische Regeln haben Vorrang vor LLM-Entscheidungen.
18. Keine neue Infrastruktur wird ohne beobachteten oder konkret begründeten Bedarf eingeführt.

---

## 28. Noch nicht durch dieses Dokument entschieden

Folgende Punkte bleiben Gegenstand späterer Arbeitspakete:

- endgültiger MVP-Scope;
- konkrete Command- und Event-Payloads;
- endgültige State Machines;
- konkrete Application-Port-Signaturen;
- genaue Transaktionsgrenze für Turn Completion;
- konkrete Projection-Implementierung;
- genaue Snapshot Policy;
- Frontend-Technologie;
- konkreter erster gerichteter Interaction Mode;
- konkrete Actor-Selection-Formel;
- konkrete Context Budgets;
- Ergebnis des DDD.BuildingBlocks-Reviews;
- exakte lokale und k3s-Datenbankkonfiguration.

Diese offenen Punkte dürfen nicht stillschweigend von der Implementierung vorweggenommen werden.

---

## 29. Regel für zukünftige Architecture Decisions

Eine neue wesentliche Entscheidung wird als kurzer ADR-Eintrag dokumentiert mit:

```text
ID und Titel
Status
Kontext
Entscheidung
relevante Alternativen
Konsequenzen und Trade-offs
ersetzte frühere Entscheidung, falls vorhanden
Datum
```

Mögliche Statuswerte:

```text
Proposed
Accepted
Superseded
Rejected
Deferred
```

Ein ADR soll nur für Entscheidungen verwendet werden, die Architektur, Abhängigkeitsrichtung, Persistenz, Runtime, externe Schnittstellen oder langfristige Änderbarkeit wesentlich beeinflussen.

---

## 30. Abschluss von Arbeitspaket 1

Mit `00-Project-Vision.md` und diesem Dokument sind folgende Ziele erreicht:

- das fachliche Zielbild ist konsolidiert;
- die Dokumente besitzen definierte Rollen;
- bekannte Architekturwidersprüche sind aufgelöst;
- Event Sourcing für alle fachlichen Aggregate und CQRS sind verbindlich;
- Source-of-Truth-Regeln sind präzisiert;
- DDD.BuildingBlocks ist als direkte taktische Grundlage bestätigt;
- frühere Persistenzannahmen sind ausdrücklich ersetzt;
- offene Hypothesen sind von akzeptierten Entscheidungen getrennt;
- zukünftige Architekturentscheidungen erhalten ein einheitliches Format.

Damit kann Arbeitspaket 1 nach inhaltlicher Abnahme als `READY` markiert werden.

# Persona Simulation Playground

## Arbeitspaket 7: Frontend- und Bedienkonzept für den MVP

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16

## 1. Zielbild

Das Web-UI ist ein unabhängiger Beobachtungs- und Steuerungsclient für die zentral gehostete Single-User-Anwendung. Es visualisiert serverseitigen Zustand und sendet Commands. Es besitzt keine fachliche Autorität und ist kein Lebenszyklusanker einer Session.

Eine gestartete Session läuft ohne geöffneten Browser weiter. Nach Reload, Tabwechsel oder Gerätewechsel liest das UI den aktuellen Zustand neu und setzt die Beobachtung über SSE fort.

## 2. Verbindliche UI-Invariants

1. Kein fachlich relevanter Zustand lebt ausschließlich im Browser.
2. Commands adressieren Sessions explizit und enthalten bei Bedarf Expected Version oder Idempotency Key.
3. Das UI darf zulässige Aktionen vorfiltern, der Server validiert verbindlich.
4. Mehrere Tabs sind mehrere Ansichten auf denselben Serverzustand.
5. Ein hängender Turn blockiert weder Navigation noch Admin-Commands.
6. Persistierte Events und temporäre Streaming-Tokens sind sichtbar verschieden.
7. Debug-Daten enthalten keine Chain of Thought.
8. UI-Frameworktypen gelangen nicht in Domain oder Application.
9. Die Anwendung bleibt vollständig über API-Verträge steuerbar.
10. Die Technologieentscheidung wird erst an einem eigenen Gate getroffen.

## 3. Informationsarchitektur

| Bereich | Zweck | MVP-Priorität |
|---|---|---|
| Dashboard | aktiver Lauf, Gesundheitszustand, letzte Aktivität | MUST |
| Personas | Definitionen und unveränderliche Versionen pflegen | MUST, zunächst funktional |
| Scenarios | Scenario und Versionen pflegen | MUST, zunächst funktional |
| Relationships | Ausgangsbeziehungen konfigurieren | MUST, einfache Form |
| Session Setup | Versionen, Mode und Limits auswählen | MUST |
| Session Player | beobachten und steuern | MUST, zentraler Screen |
| Session History | gespeicherte Sessions laden und fortsetzen | MUST |
| Diagnostics | Selection, Context Blocks, Runtime, Projection | MUST für Entwicklung und MVP-Diagnose |
| Evaluation | Fixtures und Vergleichsläufe | SHOULD |
| Settings | Endpoint, Auth, technische Konfiguration | SHOULD, Secrets geschützt |

## 4. Zentrale User Flows

### 4.1 Experiment vorbereiten

```text
Persona-Versionen wählen oder erstellen
-> Scenario-Version wählen oder erstellen
-> Relationships setzen
-> FreeSociety und Limits konfigurieren
-> Validierung und CanStart prüfen
-> Session speichern
```

Das UI darf einen Entwurf halten. Die kanonische gespeicherte Konfiguration entsteht erst durch einen erfolgreichen Server-Command.

### 4.2 Session beobachten

```text
Session Player öffnen
-> aktuelles Read Model laden
-> Live Cursor übernehmen
-> SSE abonnieren
-> neue persistierte Ereignisse anzeigen
-> bei Lücke ab Cursor nachladen
```

Ein SSE-Signal ist kein fachlicher Zustand. Nach einem Signal oder Reconnect wird der autoritative Query-Stand gelesen.

### 4.3 Steuern und intervenieren

- Start;
- Pause;
- Resume;
- genau einen Schritt ausführen;
- regulär Complete;
- Abort mit Bestätigung;
- Human Message;
- Scenario Event;
- kontrollierte Recovery-Entscheidung nach unterbrochenem Turn.

Destruktive oder terminale Aktionen müssen klar von reversiblen Aktionen unterscheidbar sein.

### 4.4 Persona während einer Session ändern

Globale Persona-Versionen sind nach ihrer Verwendung unveränderlich. Eine Bearbeitung erzeugt eine neue Version und verändert die laufende Session nicht rückwirkend.

Sessionbezogene Zustände, etwa Mood oder Relationship, werden über Session-Commands verändert, sofern der MVP dafür einen expliziten Flow vorsieht. Das UI darf globale Definition und Sessionzustand nicht vermischen.

## 5. Session Player

Der Player ist die wichtigste MVP-Ansicht.

### Hauptbereich

- chronologischer Feed mit originalen Persona-Aussagen;
- Actor, Zeitpunkt, Turn und Ereignisart;
- deutliche Markierung von Human Messages und Scenario Events;
- Anzeige nur committed Beiträge als dauerhaft;
- temporäres Token Streaming optional und visuell vorläufig.

### Steuerbereich

- Sessionstatus;
- Start, Pause, Resume, Step, Complete und Abort gemäß Server-Capabilities;
- Human Input;
- Scenario Event Injection;
- sichtbarer laufender Turn mit Cancel- oder Recovery-Option, sofern serverseitig zulässig.

### Beobachtungsbereich

- aktiver Participant;
- letzter Auswahlgrund;
- Turn- und Tokenlimits;
- Inference- und Runtimezustand;
- Projection Lag;
- Verbindung und letzter synchronisierter Cursor.

## 6. Debug- und Diagnoseansichten

Normalbetrieb und Diagnose werden getrennt dargestellt, dürfen aber denselben serverseitigen Zustand lesen.

Zulässige Diagnoseinformationen:

- Candidate Set und regelbasierte Filter;
- ausgewählter Actor und Selection Scores;
- Context-Block-Namen, Reihenfolge, Tokenzahlen und Kürzungen;
- tatsächlich an das Modell übertragener Prompt, soweit Secrets und sensible Konfiguration entfernt sind;
- Structured Output;
- Validation- und Retry-Ergebnis;
- CorrelationId, TurnId und erwartete Sessionversion;
- Projection Checkpoint und Lag;
- Health der externen Abhängigkeiten.

Nicht vorgesehen:

- versteckte Reasoning-Tokens;
- Chain of Thought;
- unmaskierte Credentials;
- Datenbankzugriff aus dem Browser;
- technische Mutationen unter Umgehung von Commands.

## 7. Mehrere Tabs, Reconnect und Konflikte

Jeder Tab:

1. authentifiziert sich mit derselben einzigen Benutzeridentität;
2. lädt sein Read Model selbst;
3. hält einen eigenen SSE-Cursor;
4. sendet Commands mit Versions- oder Idempotency-Information;
5. behandelt `409 Conflict` als erwartbaren Synchronisationsfall;
6. lädt nach einem Konflikt den aktuellen Serverzustand neu.

Es gibt kein Tab-Leader-Election-Verfahren und keinen Browser-Lock. Die Kontrolle liegt beim Server.

## 8. Fehler- und Recovery-UX

Fehler werden mindestens unterschieden in:

| Kategorie | UI-Verhalten |
|---|---|
| Validation | Eingabe konkret markieren |
| Domain Rejection | Grund und aktuellen Zustand zeigen |
| Concurrency Conflict | neu laden, Änderung nicht still wiederholen |
| Inference Failure | Sessionstatus erhalten, Retry-/Pause-Option zeigen |
| Projection Lag | Datenstand und Nachlauf sichtbar machen |
| Dependency Outage | betroffene Funktion kennzeichnen, UI bedienbar halten |
| Auth Failure | erneute Anmeldung, keine Endlosschleife |

Ein globaler Full-Screen-Spinner darf nicht die gesamte Anwendung blockieren, nur weil ein Turn läuft.

## 9. Authentifizierung

Für den MVP existiert genau eine berechtigte Identität, aber kein anonymer Heimnetzzugriff. Der konkrete Mechanismus wird zusammen mit Deployment und Secret Handling gewählt.

Mögliche Varianten:

- lokale ASP.NET-Core-Identity mit einem Account;
- vorgeschalteter Identity-Aware Proxy im Homelab;
- standardisierte OIDC-Anbindung an einen vorhandenen Identity Provider.

Nicht empfohlen ist ein statisches API-Token im Browser-Quellcode oder in öffentlich ausgelieferten Konfigurationsdateien.

## 10. Technologieoffene Kandidaten

### Blazor WebAssembly oder Blazor Web App

Passt gut zu C#-Know-how, gemeinsam nutzbaren DTOs und einem .NET-zentrierten Repository. Zu prüfen sind langfristige UI-Ergonomie, Clientgröße, Debugging und die saubere Trennung vom Serverprozess. Blazor Server mit verbindungsgebundenem UI-State wäre für das Zielbild weniger attraktiv, weil die UI unabhängig und reconnect-fähig bleiben soll.

### React mit TypeScript

Sehr großes Ökosystem, gute Unterstützung für komplexe Clientzustände, Formulare, SSE und Testwerkzeuge. Der Preis ist ein zweiter Technologie-Stack und die Notwendigkeit, API-Verträge sauber zu generieren oder synchron zu halten.

### Vue mit TypeScript

Gute Balance aus überschaubarer Komplexität und moderner Komponentenarchitektur. Eignet sich für ein kontrolliertes Einzelentwicklerprojekt. Ökosystem und Teamverfügbarkeit sind kleiner als bei React, für dieses Projekt aber wahrscheinlich ausreichend.

### Svelte beziehungsweise SvelteKit

Kompakt und reaktiv, attraktiv für einen schlanken Client. Das kleinere .NET-Integrationsökosystem und langfristige Wartungsrisiko müssen gegen die geringe UI-Komplexität abgewogen werden.

### Avalonia als Desktop-Client

Interessant, falls später ein nativer Linux-/Windows-Client gewünscht wird. Für den aktuellen browserbasierten Zugriff auf die zentrale Anwendung erhöht ein paralleler Desktop-Stack jedoch den Aufwand und ist kein MVP-Favorit.

## 11. Entscheidungskriterien und Gate

Ein kleiner UI-Spike soll mit zwei realistischen Kandidaten dieselben Aufgaben demonstrieren:

- Login;
- Query laden;
- SSE reconnect;
- Command mit Conflict Handling;
- Formular mit Validation;
- Session Feed mit mindestens 1000 Events;
- Component- und E2E-Test;
- produktiver Build und Container-Auslieferung.

Bewertet werden:

| Kriterium | Bedeutung |
|---|---|
| serverseitige Entkopplung | kein versteckter Sessionbesitz im UI |
| Entwicklungsproduktivität | verständlich und schnell änderbar |
| Testbarkeit | Komponenten- und Browser-E2E |
| SSE und Reconnect | robuste Liveansicht |
| Formulare | Persona- und Scenario-Editoren |
| Diagnoseansichten | große strukturierte Datenmengen |
| Deployment | statische Assets oder klarer Web-Host |
| Wartbarkeit | überschaubare Dependencies und Upgrades |

Meine vorläufige Shortlist ist Blazor WebAssembly/Web App gegen React oder Vue mit TypeScript. Eine endgültige Wahl ohne Spike wäre derzeit unnötig früh.

## 12. API-Anforderungen aus UI-Sicht

- explizite Commands und Queries;
- Capability- beziehungsweise Allowed-Actions-Information im Read Model;
- konsistentes Problem-Details-Format;
- Expected Version und Idempotency;
- paginierte Historie;
- SSE mit monotonem Cursor und Replay;
- Health und Dependency Status;
- Projection Lag;
- Context- und Selection-Diagnose über geschützte Endpunkte;
- keine direkte Abhängigkeit des Clients von Event-Store-Schemata.

## 13. Tests

- Komponenten-Tests für Formulare und Statusdarstellung;
- Contract-Tests gegen API-Schemas;
- Browser-E2E für die zentralen User Flows;
- Reconnect- und Multi-Tab-Tests;
- Accessibility-Smoke-Tests;
- Tests für lange Eventfeeds;
- Security-Tests für unautorisierten Zugriff und Secret Leakage.

## 14. Bewusst vertagt

- endgültige Frameworkwahl;
- Pixel Design und Branding;
- nativer Desktop-Client;
- Mobile App;
- kollaborative Mehrbenutzerfunktionen;
- Offline Editing;
- komplexe Visualisierungen sozialer Graphen;
- dauerhafte Speicherung beliebiger UI-Layouts.

## 15. Abnahmekriterien für Arbeitspaket 7

- UI ist als austauschbarer Client definiert.
- Screens und MVP-Flows sind abgegrenzt.
- Session läuft unabhängig vom Browser.
- Multi-Tab-, Reconnect- und Conflict-Verhalten sind beschrieben.
- Persona-Version und Sessionzustand werden nicht vermischt.
- Technologieentscheidung ist bewusst auf ein messbares Spike-Gate verschoben.
- geeignete Kandidaten und Bewertungskriterien liegen vor.

## 16. Technische Referenzen für die spätere Kandidatenprüfung

- [ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-10.0)
- [Blazor Hosting und Render Modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0)
- [React Dokumentation](https://react.dev/)
- [React mit TypeScript](https://react.dev/learn/typescript)
- [Vue mit TypeScript](https://vuejs.org/guide/typescript/overview.html)
- [Avalonia Dokumentation](https://docs.avaloniaui.net/docs/welcome)

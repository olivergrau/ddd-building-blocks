# Persona Simulation Playground

## Dokumentübergreifende Konsistenzprüfung

**Status:** REVIEW DRAFT  
**Version:** 1.0  
**Datum:** 2026-08-16

## 1. Prüfumfang

Geprüft wurden die konsolidierten Entscheidungen, Arbeitspakete 3 bis 8, die Frameworkanalyse sowie der Implementierungsplan. Ältere Quelldokumente bleiben Detailquellen, werden bei Konflikten aber von den aktuellen Entscheidungsdokumenten überstimmt.

## 2. Bestätigte konsistente Kernaussagen

| Thema | Verbindlicher Stand |
|---|---|
| Architektur | modularer Monolith in einem .NET-Prozess |
| Domain | rein fachlich, taktische DDD.BuildingBlocks erlaubt |
| Persistenz | Event Sourcing für alle fachlichen Aggregate; konventionelle Persistenz nur für technische und abgeleitete Daten |
| CQRS | gesetzt, Read Models abgeleitet und rebuildbar |
| Runtime | serverseitig autoritativ, UI-unabhängig |
| Nutzer | eine berechtigte Identität, keine Multi-Tenancy |
| Sessions | viele speicherbar, genau eine autonom aktiv |
| Inference | global seriell, vLLM extern |
| Kontext | pro Request persona-spezifische Sicht, aus serverseitigem Zustand rekonstruierbar |
| KV-Cache | flüchtige Optimierung, niemals Source of Truth |
| Interaction Mode | Architektur erweiterbar, MVP liefert nur FreeSociety |
| UI | austauschbarer Webclient, Framework noch offen |
| Framework | zuerst modernisieren und ausdrücklich freigeben |

## 3. Nachgeschärfte Punkte

### 3.1 PostgreSQL-Zeitpunkt

Die alte Formulierung, PostgreSQL erst nach dem vertikalen Slice einzuführen, wäre mit der Entscheidung „PostgreSQL von Anfang an“ missverständlich.

Präzisierung:

- reale PostgreSQL- beziehungsweise Produktprovider-Integration beginnt früh im Framework- und Persistenztrack;
- Containerisierung und k3s-Deployment erfolgen weiterhin erst nach einem lokal funktionierenden vertikalen Slice;
- Tests dürfen In-Memory verwenden, reale Providerintegration bleibt zusätzlich verpflichtend.

### 3.2 Event-Store-Technologie

PostgreSQL ist für Read Models und technische Persistenz gesetzt. Ob die Event Streams aller fachlichen Aggregate ebenfalls PostgreSQL oder eine externe spezialisierte Lösung verwenden, wird am frühen Gate F6 entschieden.

Dies ist kein Widerspruch, solange:

- Application und Domain providerneutral bleiben;
- nur ein Produktprovider für den MVP implementiert wird;
- Betriebsaufwand Teil der Entscheidung ist.

### 3.3 Testabdeckung

„Möglichst hoch“ wird als risikobasierte, mehrstufige Abdeckung interpretiert. Eine pauschale Prozentzahl wäre vor Baseline und erstem Slice nicht belastbar. Prozentgates werden später empirisch festgelegt, kritische Branches müssen unabhängig davon geprüft werden.

### 3.4 UI-Technologie

Der frühere Abschlusswert „Frontend-Technologie ist entschieden“ wird präzisiert: Vor Backend-Beginn genügt ein technologieoffenes Konzept. Vor U1 ist ein kleiner Vergleichsspike und eine ausdrückliche Entscheidung erforderlich.

### 3.5 Arbeitspaket 8 und Implementierungsreihenfolge

Frameworkanalyse und Modernisierungsplan existieren bereits. Das neue Gate 08C verhindert, dass „analysiert“ mit „produktionsbereit“ verwechselt wird. Die tatsächliche Modernisierung ist der erste Implementierungstrack.

## 4. Bewusst offene, nicht blockierende Entscheidungen

- konkretes UI-Framework;
- konkrete Event-Store-Technologie;
- konkrete Authentifizierungsvariante;
- finale Coverage-Prozentwerte;
- Behavioral-Schwellenwerte;
- Snapshot-Aktivierung und Intervall;
- genaue Actor-Selection-Gewichte;
- optionale Token-Streaming-Darstellung.

Diese Entscheidungen besitzen jeweils ein Gate vor dem ersten Code, der sie zwingend benötigt.

## 5. Blockierende Entscheidungen vor dem ersten Code

Es bestehen keine fachlichen Blocker für F0. F0 verändert keine Semantik und reproduziert ausschließlich den Framework-Ausgangszustand.

Vor späteren Etappen blockieren jeweils:

- F6: Produktproviderentscheidung;
- U1: UI-Technologieentscheidung nach Spike;
- externer Zugriff: Authentifizierungsentscheidung;
- Behavioral Gate: empirische Baseline und akzeptierte Rubriken.

## 6. Ergebnis

Der Dokumentensatz ist auf konzeptioneller Ebene implementierungsfähig. Er schreibt nicht jede Signatur vor, verhindert aber, dass Codex wesentliche Architektur-, Scope- oder Produktentscheidungen selbstständig treffen muss.

Der nächste sinnvolle Schritt nach fachlicher Abnahme dieser Dokumente ist die Vorbereitung des konkreten F0-Auftrags, nicht die parallele Erstellung des gesamten Repositorys.

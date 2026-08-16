# Persona Simulation Playground

## Project Vision

**Status:** Verbindliches Projektdokument  
**Version:** 1.0  
**Datum:** 2026-08-15  
**Geltungsbereich:** Produktvision, fachliches Zielbild, Leitprinzipien und langfristige Abgrenzung

---

## 1. Zweck des Projekts

Der Persona Simulation Playground ist eine lokale, webbasierte Umgebung zur Simulation sozialer Interaktionen mehrerer KI-gesteuerter Personas.

Die Plattform ermöglicht es einem Menschen, wiederverwendbare Personas, Szenarien und Interaktionsregeln zu definieren, daraus konkrete Simulationen zu erzeugen, deren Verlauf zu beobachten und jederzeit kontrolliert einzugreifen.

Das System ist weder lediglich ein Multi-Persona-Chat noch eine Plattform für allgemeine autonome Agenten.

Das fachliche Zielbild lautet:

> Eine lokale Simulationsumgebung für soziale Interaktionen mehrerer KI-Personas, in der der Mensch Personas, Szenarien und Interaktionsregeln definiert, die entstehende Interaktion beobachtet und jederzeit beeinflussen kann.

Der Playground besitzt bewusst einen spielerischen und experimentellen Charakter. Er soll überraschende, unterhaltsame und explorative soziale Situationen erzeugen können. Gleichzeitig soll seine Architektur allgemein genug sein, um praktisch nutzbare Formate wie ein beratendes Persona-Gremium zu unterstützen.

---

## 2. Zentrale Produkthypothese

Die erste zu prüfende Hypothese lautet:

> Sauber definierte und kontrolliert orchestrierte LLM-Personas können über längere Interaktionen als unterscheidbare Charaktere wahrgenommen werden und dabei interessante, unterhaltsame oder praktisch nützliche soziale Dynamiken erzeugen.

Das zentrale Qualitätskriterium ist daher nicht die bloße Menge erzeugter Nachrichten.

Entscheidend ist:

- Personas bleiben erkennbar unterschiedlich;
- sie reagieren kontextbezogen;
- sie berücksichtigen Beziehungen und frühere Ereignisse;
- sie handeln innerhalb der Regeln der simulierten Situation;
- die entstehende Interaktion wirkt nicht wie ein serieller Monolog desselben Assistenten;
- Schweigen oder Nicht-Handeln darf ein gültiger sozialer Zustand sein.

---

## 3. Kernkonzepte

Die Plattform trennt drei grundlegende fachliche Konzepte:

### 3.1 Persona

Eine Persona beschreibt, wer innerhalb einer Simulation handelt.

Sie besitzt eine wiederverwendbare und versionierte Definition, beispielsweise:

- Identität und Hintergrund;
- Rolle oder Beruf;
- Interessen und Expertise;
- Ziele und Überzeugungen;
- Präferenzen und Abneigungen;
- Kommunikationsstil;
- strukturiertes psychologisches Profil;
- Beziehungen zu anderen Personas.

Personas können original, archetypisch, historisch simuliert oder fiktional simuliert sein. Historische und fiktionale Personas sind ausdrücklich Simulationen auf Grundlage ihrer jeweiligen Definition.

### 3.2 Scenario

Ein Scenario beschreibt die Situation oder simulierte Welt, in der eine Interaktion stattfindet.

Es kann beispielsweise definieren:

- Ausgangssituation;
- Thema;
- Umgebung;
- Rollenanforderungen;
- bekannte Fakten;
- mögliche Ereignisse;
- fachliche Grenzen.

Scenarios sind wiederverwendbar und versioniert.

### 3.3 Interaction Mode

Ein Interaction Mode beschreibt die sozialen und organisatorischen Regeln einer Interaktion.

Beispiele:

- Gremium;
- Talkrunde;
- Debatte;
- freie Gesellschaft;
- spätere ereignisorientierte Rollenspiel- oder Simulationsmodi.

Der Interaction Mode entscheidet nicht über die Überzeugungen oder konkrete Ausdrucksweise einer Persona. Er definiert den zulässigen sozialen Rahmen, Phasen, Rollen, Handlungsmöglichkeiten und Abschlussbedingungen.

### 3.4 Session

Eine Session ist eine konkrete Simulation.

Sie verbindet:

```text
Persona-Versionen
+ Scenario-Version
+ Interaction-Mode-Konfiguration
+ Runtime-Limits
+ optionales Thema
+ Human-Interaktion
```

Die Session besitzt einen persistenten und nachvollziehbaren fachlichen Verlauf.

### 3.5 Participant

Ein Participant ist die sessiongebundene Ausprägung einer konkreten Persona-Version.

Er besitzt zusätzlich zum stabilen Persona-Profil dynamischen Sessionzustand, beispielsweise:

- Anwesenheit;
- aktuellen Fokus;
- Stimmung;
- kurzfristiges Ziel;
- Aktivierungs- und Recency-Zustand;
- sessionbezogene Beziehungen.

Die langfristige Persona und ihr aktueller Participant-Zustand bleiben getrennt.

### 3.6 Relationship

Beziehungen sind ein Kernmerkmal der Plattform.

Es wird unterschieden zwischen:

- langfristiger Relationship Baseline zwischen Personas;
- dynamischem Relationship State zwischen Participants innerhalb einer Session.

Sessionbezogene Veränderungen wirken nicht automatisch auf die langfristige Relationship Baseline zurück. Eine spätere Konsolidierung muss eine ausdrückliche fachliche Funktion sein.

### 3.7 Event

Die Simulation wird fachlich als Folge relevanter Ereignisse verstanden.

Events können unter anderem ausdrücken:

- eine Persona spricht oder reagiert;
- ein Mensch spricht;
- ein Participant betritt oder verlässt die Situation;
- ein Scenario Event tritt ein;
- eine Session wird gestartet, pausiert, fortgesetzt oder beendet;
- ein Interaction Mode wechselt seine Phase.

Technische Vorgänge wie HTTP-Aufrufe, Cache Hits oder Projection Retries sind keine Ereignisse der simulierten Welt.

---

## 4. Menschliche Rollen und Kontrolle

Der Mensch kann je nach Session unterschiedliche Rollen einnehmen:

- Zuschauer;
- Teilnehmer;
- Moderator;
- Regisseur oder Administrator.

Human-Intervention ist ein wesentliches Produktmerkmal und kein nachträglicher Sonderfall.

Der Mensch soll insbesondere:

- Nachrichten an die Simulation senden;
- Scenario Events injizieren;
- Sessions pausieren und fortsetzen;
- Sessions kontrolliert beenden;
- einzelne Runtime-Schritte auslösen;
- den Verlauf und relevante Entscheidungsinformationen untersuchen können.

Administrative Commands und sichtbare Human-Nachrichten werden fachlich getrennt.

---

## 5. Verantwortung von Domain, Orchestrierung und LLM

Die Plattform verbindet deterministische Steuerung mit probabilistischer LLM-Interpretation.

Die gewünschte Verantwortungsverteilung lautet:

```text
DOMAIN

definiert Zustand, Invariants, erlaubte Handlungen
und fachlich eingetretene Ereignisse


INTERACTION MODE

definiert soziale Regeln, Phasen, Rollen,
Handlungsmöglichkeiten und Abschlussbedingungen


ORCHESTRIERUNG

entscheidet, wann ein Schritt ausgeführt wird,
wer eine Handlungsmöglichkeit erhält und
welche Prioritäten gelten


LLM

entscheidet innerhalb des erlaubten Rahmens,
was eine Persona situativ tun möchte und
wie sie dies konkret ausdrückt
```

Das LLM verwaltet nicht:

- Session Lifecycle;
- Persistenz;
- Event Routing;
- Turn Scheduling;
- Concurrency;
- Human Priority;
- technische Recovery;
- Domain Invariants.

Das LLM ist eine Inference-Komponente und nicht die Runtime der Anwendung.

---

## 6. Psychologisches und kommunikatives Modell

Personas sollen nicht ausschließlich als große freie Systemprompts modelliert werden.

Eine Persona-Version kombiniert strukturierte und natürlichsprachliche Bestandteile:

```text
Persona Identity
+ Psychological Profile
+ Communication Profile
+ Goals
+ Beliefs
+ Interests und Expertise
+ optionale freie Beschreibung
```

Numerische Traits sind konfigurierbare Verhaltensneigungen. Sie stellen keine wissenschaftliche Diagnose und kein empirisch validiertes menschliches Persönlichkeitsmodell dar.

Die Trait-Werte dienen zwei getrennten Verwendungswegen:

1. begrenzte deterministische Signale für die Orchestrierung;
2. deterministische Übersetzung in semantische Verhaltenshinweise für das LLM.

Der konkrete Nutzen jedes Traits wird nicht vorausgesetzt, sondern evaluiert. Traits ohne stabilen beobachtbaren Zusatznutzen können vereinfacht oder entfernt werden.

---

## 7. Gerichtete und emergente Simulation

Die Plattform unterstützt zwei grundlegende Interaktionsformen.

### Gerichtete Simulation

Ein Interaction Mode besitzt definierte Rollen, Phasen oder Ziele.

Beispiele:

- Architektur-Gremium;
- moderierte Talkrunde;
- strukturierte Debatte.

### Emergente Simulation

Die soziale Dynamik entsteht stärker aus Personas, Beziehungen, Ereignissen und situativer Aktivierung.

Beispiel:

- freie Gesellschaft auf einem Dorfplatz.

Die Architektur darf nicht auf nur einen dieser Fälle zugeschnitten werden. Ein erster freier Referenzfall und ein späterer gerichteter Referenzfall sollen prüfen, ob die gemeinsamen Abstraktionen tatsächlich tragen.

---

## 8. Lokaler Betrieb und Sicherheitsgrenze

Der Playground wird vollständig im privaten Homelab betrieben.

Die Anwendung verwendet einen vorhandenen lokalen vLLM-Endpunkt. Die eigentliche LLM-Inference läuft getrennt vom Application Tier auf dem DGX Spark.

Für den MVP besitzen Personas keine externen Tools und führen keine Aktionen außerhalb der Simulation aus.

Insbesondere nicht Bestandteil des MVP sind:

- Internetrecherche durch Personas;
- Shell-Zugriff durch Personas;
- E-Mail oder externe Kommunikation;
- selbst erzeugte Tools;
- selbstständige Veränderung des Systems;
- ungeprüfte autonome Agentenketten.

---

## 9. Qualitätsziele

Die Plattform soll folgende Qualitätsmerkmale erreichen:

- **Persona-Unterscheidbarkeit:** Beiträge bleiben den jeweiligen Charakteren zuordenbar.
- **Persona-Konsistenz:** Verhalten bleibt über eine Session hinweg plausibel.
- **Kontexttreue:** Relevante frühere Aussagen und Ereignisse werden berücksichtigt.
- **Beziehungssensitivität:** Unterschiedliche Beziehungen führen zu beobachtbar unterschiedlichem Verhalten.
- **Szenario-Treue:** Personas handeln innerhalb der simulierten Situation.
- **Interaktionsqualität:** Beiträge reagieren aufeinander und bilden keine isolierten Monologe.
- **Konfliktfähigkeit:** Widerspruch und Meinungsverschiedenheit werden nicht künstlich vermieden.
- **Human-Steuerbarkeit:** Der Mensch kann den Ablauf jederzeit nachvollziehbar beeinflussen.
- **Begrenzbarkeit:** Runtime-, Turn- und Tokenlimits verhindern unkontrollierte Schleifen.
- **Rekonstruierbarkeit:** Fachlicher Zustand überlebt Anwendungs- und Inference-Neustarts.
- **Nachvollziehbarkeit:** Auswahl, Kontextaufbau und fachliche Resultate können untersucht werden.

---

## 10. Anti-Ziele

Folgende Ergebnisse gelten ausdrücklich nicht als gewünschte Simulation:

- alle Personas sprechen in starrer Reihenfolge;
- jede Persona muss bei jeder Gelegenheit sprechen;
- alle Personas stimmen einander reflexartig zu;
- alle Beiträge klingen wie derselbe generische Assistent;
- Rollen werden als eindimensionale Karikaturen gespielt;
- der Orchestrator erfindet selbst den Inhalt der Simulation;
- das LLM entscheidet eigenständig über Regeln und Runtime;
- technische Agentenautonomie wird als soziale Glaubwürdigkeit inszeniert;
- die Simulation läuft ohne kontrollierbare Grenzen endlos weiter.

---

## 11. Erste fachliche Referenzfälle

### Freie Gesellschaft

Eine kleine Dorfplatz- oder Kneipensituation mit drei deutlich unterschiedlichen Personas.

Zweck:

- freie Sprecherwahl;
- Persona-Unterscheidbarkeit;
- Reaktionen auf andere Personas;
- Human- und Scenario-Intervention;
- Beobachtung emergenter Dynamik.

### Gerichteter Interaction Mode

Ein Gremium oder eine moderierte Talkrunde.

Zweck:

- Rollen und Phasen;
- zielgerichtete Diskussion;
- Abschlussbedingungen;
- Prüfung, ob dieselben Kernabstraktionen für freie und gerichtete Interaktion tragen.

Die genaue MVP-Zuordnung wird im separaten Scope-Dokument festgelegt.

---

## 12. Langfristiger Ideenraum

Spätere Erweiterungen können umfassen:

- persistentes Persona-Memory über Sessions;
- persistente Relationship Memories;
- bewusste Konsolidierung von Sessionerfahrungen;
- persistente simulierte Welten;
- mehrere Orte und selektive Wahrnehmung;
- Persona- oder Scenario-spezifisches Knowledge/RAG;
- unterschiedliche Modelle für unterschiedliche Personas;
- Session Branching und Regeneration;
- automatische Session-Zusammenfassungen;
- langfristige Behavioral Evaluation;
- Visualisierung sozialer Beziehungen;
- zusätzliche Interaction Modes;
- zeitgesteuerte Scenario Events.

Diese Punkte gehören zur Produktvision, sind aber keine impliziten Anforderungen an den MVP.

---

## 13. Leitprinzip

Bei jeder Produkt- und Architekturentscheidung gilt:

> Unterstützt die Entscheidung eine kontrollierbare, nachvollziehbare und sozial interessante Persona-Simulation oder erzeugt sie lediglich zusätzliche Infrastruktur beziehungsweise Autonomie-Theater?

Der Playground soll klein beginnen, empirisch lernen und nur aufgrund beobachteter Anforderungen komplexer werden.


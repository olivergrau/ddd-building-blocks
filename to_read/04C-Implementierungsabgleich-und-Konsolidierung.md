# Persona Simulation Playground

## Arbeitspaket 4C: Implementierungsabgleich und Konsolidierung

**Status:** OPEN, fortlaufendes Arbeitsdokument  
**Version:** 0.1  
**Datum:** 2026-08-16  
**Zweck:** Application-Ports und Verantwortungsgrenzen anhand kleiner C#-Slices validieren

## 1. Prinzip

4A ist die konzeptuelle Vorgabe. 4B liefert Beispiele. 4C hält fest, was die tatsächliche Implementierung bestätigt oder begründet verändert.

Keine Signatur aus 4B ist allein deshalb verbindlich, weil sie bereits C#-förmig aussieht.

## 2. Änderungsprotokoll

| ID | Datum | Komponente/Port | Beobachtung | Optionen | Entscheidung | Auswirkung | Abgenommen |
|---|---|---|---|---|---|---|---|
| P-001 | offen | offen | offen | offen | offen | offen | nein |

## 3. Zu validierende Hypothesen

| Hypothese | Spike/Test | Erfolgskriterium | Status |
|---|---|---|---|
| Framework-Repository kann direkt in Application genutzt werden | minimaler Session Handler | kein technischer Leak, kein sinnloser Wrapper | offen |
| Einheitlicher Event-Store-Vertrag trägt alle fachlichen Aggregate | Persona- und Scenario-Slice | klare Fachsprache ohne parallele CRUD-Write-Infrastruktur | offen |
| Ein Runtime-Impuls ist eine gute Transaktionsgrenze | Fake-Inference-Slice | höchstens eine fachliche Aktion, sauberer Stop | offen |
| WHO und Decision Service bleiben unabhängig | deterministische Selection Tests | Selection ohne LLM testbar | offen |
| ContextBuilder benötigt keine Aggregate | Context-Slice | Input über expliziten TurnContext vollständig | offen |
| Turn State muss persistiert werden | Restart-Simulation | Recovery ohne verlorene oder doppelte Action | offen |
| Live Update nach Projection ist praktikabel | erste Session Projection | UI kann Zustand beim Signal sofort lesen | offen |
| Providerwechsel bleibt außerhalb von Domain/Application | InMemory gegen Produktprovider Contract Tests | gleiche Verhaltenssuite | offen |

## 4. Geplante Validierungsetappen

### P1: Event-sourced Persona Use Case

- Application Command;
- Persona Store beziehungsweise Framework-Repository-Contract;
- In-Memory Event Provider;
- Expected Stream Version;
- kein API-Projekt.

**Prüffrage:** Liefert ein anwendungsfachlicher Port echten Mehrwert oder wäre er nur ein durchleitender Wrapper um den Framework-Repository-Contract?

### P2: Session Command ohne Runtime

- Create, Configure und Start;
- Session Persistence Capability;
- In-Memory Event Provider;
- Expected Version;
- Domain Events erst nach Aggregateentscheidung.

**Prüffrage:** Direkter Framework-Contract oder `ISessionStore`?

### P3: Projection Slice

- committed Feed;
- ein Session Read Model;
- Checkpoint;
- doppelte Zustellung;
- Rebuild.

**Prüffrage:** Welche Projektionsfähigkeit gehört ins Framework und welche in die Playground Application?

### P4: Fake Participant Turn

- Runtime Impulse;
- FreeSociety Test Mode/Policy;
- deterministische Selection;
- ContextBuilder Stub;
- Fake Decision Service;
- `Speak` und `DoNothing`;
- stale-result test.

**Prüffrage:** Sind Orchestrator und Runtime Coordinator sauber getrennt?

### P5: Inference Adapter

- providerneutraler Request;
- vLLM Adapter;
- Structured Output Parsing;
- Cancellation und Timeout;
- kein Domainwissen im Adapter.

### P6: Active-Session und Restart Recovery

- genau eine aktive Session;
- Prozessabbruch während Turn;
- `RecoveryRequired`;
- UI/Admin weiterhin erreichbar;
- kein ungeprüftes Auto-Resume.

## 5. Persistence Decision Gate

Vor Implementierung des produktiven Event Stores wird eine Entscheidung dokumentiert:

```text
Option A: DDD.BuildingBlocks PostgreSQL Provider
Option B: adaptierte relationale Providerlogik
Option C: externer Event Store mit Adapter
```

Erforderliche Nachweise:

- atomarer Expected-Version-Append;
- globaler Projection Feed;
- Backup und Restore;
- Integrationsteststrategie;
- Betriebsaufwand im Homelab;
- Ressourcenbedarf;
- Migrations- und Lock-in-Bewertung.

Bis zu diesem Gate werden ausschließlich providerneutrale Fähigkeiten und In-Memory-Testimplementierungen verwendet.

## 6. Reviewfragen nach jeder Etappe

1. Hat jede Verantwortung weiterhin genau einen Owner?
2. Ist Fachlogik aus Handlern in die Domain gerutscht oder umgekehrt?
3. Existiert ein Interface nur für Test-Mocking ohne echte Grenze?
4. Leckt eine Infrastrukturtechnologie in Application oder Domain?
5. Wird ein Aggregate als Query Model missbraucht?
6. Ist Expected Version im gesamten Mutation Flow sichtbar?
7. Bleibt Inference außerhalb einer Transaktion?
8. Kann ein Fehler den UI-/Admin-Zugang blockieren?
9. Ist der Runtime-Impuls klein und nachvollziehbar?
10. Muss 4A oder 4B gezielt aktualisiert werden?

## 7. Statusregeln

```text
OPEN
PROVISIONAL
ACCEPTED
SUPERSEDED
```

## 8. Abschlusskriterien für Arbeitspaket 4

Arbeitspaket 4 wird `READY`, wenn:

- 4A fachlich abgenommen ist;
- 4B als ausreichende Implementierungsorientierung abgenommen ist;
- P1 bis mindestens P4 in kleinen Slices validiert wurden;
- Application und Domain keine Infrastrukturlecks enthalten;
- Persist-before-publish im Projection Slice nachgewiesen ist;
- stale-turn protection nachgewiesen ist;
- direkte Frameworknutzung versus Application Wrapper bewusst entschieden ist;
- das Persistence Decision Gate terminiert und vor produktiver Implementierung platziert ist;
- keine Portentscheidung die Event-Store-Technologie unkontrolliert vorwegnimmt.

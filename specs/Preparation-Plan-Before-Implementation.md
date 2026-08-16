# Persona Simulation Playground

## Action plan before implementation begins

**Status:** Working and control document  
**Date:** 2026-08-15  
**Purpose:** Consolidate the specification and prepare for a controlled, phased implementation with Agentic Codex

---

## 1. Purpose of this document

The professional and technical conception of the Persona Simulation Playground is well advanced. However, before actual implementation begins, some decisions need to be consolidated and translated into verifiable contracts.

This document defines the work packages that are still open. They are processed one after the other and each is explicitly completed before the next point is started.

The later implementation is expressly not carried out as a big bang order to Codex. Instead:

> Codex implements small, professionally and technically limited stages. After each stage, the result, code, tests and decisions made are checked. Only then will the next stage be released.

Three goals should be achieved at the same time:

1. Maintain control over architecture and scope.
2. Build understanding of the resulting implementation.
3. Detect undesirable developments before they spread across multiple components.

---

## 2. Editing rules

The following rules apply to each work package:

- Only the respective point is processed.
- Open decisions are visibly documented and not made silently by Codex.
- Alternatives are only considered if they are relevant to the specific decision.
- Later extensions will be marked as such and will not be pulled into the MVP.
- A work package ends with a verifiable document or a clear decision.
- Contradictions to older documents are expressly marked as being replaced or still valid.
- Implementation will not begin until packages marked as blocking are completed.

Progress status:

```text
OPEN        not started
IN REVIEW   currently being worked on
DECIDED     domain decision made
READY       sufficiently concrete for implementation
DEFERRED    deliberately postponed
```

---

# 3. Work package 1: Document hierarchy and binding decision status

**Status:** READY  
**Blocking:** Yes

**Accepted:** 2026-08-15  
**Result documents:** `00-Project-Vision.md`, `01-Architecture-Decisions.md`

## Goal

The existing documents come from several conceptual steps that build on one another. Some later decisions replace earlier assumptions. Before implementation, it must therefore be clear which statements are binding.

Particularly relevant is the development of:

```text
Current State + persistente Eventhistorie
```

to:

```text
event sourcing for all genuine domain aggregates
+ CQRS-Projektionen
```

## Points to be clarified

- Which documents constitute the current binding status?
- Which documents are basics, additions or historical interim statuses?
- Which later decisions override older statements?
- Which terms will be standardized?
- Which statements are binding architectural decisions?
- Which statements are merely hypotheses or later options?
- How will new decisions be documented in the future?

## Expected result

A compact document map containing at least the following information:

| document | role | Commitment | Replaced by / supplemented | Remark |
|---|---|---|---|---|
| Technical kickoff | Product basis | binding | supplemented by detailed documents | Target image and non-goals |
| Data model | Intermediate status | partially overhauled | Domain Model / Event Sourcing | technical terms remain partially valid |
| Technical target architecture | Architecture foundation | binding | replaced selectively | modular monolith and layer boundaries |
| Domain Model / Event Sourcing | current domain status | binding | open | Event streams are the source of truth for genuine domain aggregates |

In addition, a short rule should be defined for future Architecture Decision Records.

## Completion criteria

- [x] Every existing document has a defined role.
- [x] All known contradictions are listed.
- [x] For each contradiction it is determined which statement applies.
- [x] Binding decisions and open hypotheses are distinguishable.
- [x] A simple rule for future decision documentation is established.

---

# 4. Work package 2: Mandatory MVP scope

**Status:** READY  
**Blocking:** Yes

**Result document:** `02-MVP-Scope.md`
**Accepted:** 2026-08-15

## Goal

The MVP must be small enough to test the central product hypothesis early:

> Is it interesting, entertaining or useful to watch multiple clearly distinguishable personas in a controlled social interaction?

The detailed documents already contain numerous later extensions. Without a hard scope limit, there is a risk that these will accidentally become part of the initial implementation.

## Points to be clarified

- Which functions are absolutely necessary for the first vertical spike?
- What features are included in the full MVP after the spike?
- Which interaction mode will be implemented first?
- Is a second directed interaction mode needed in the MVP or is it deliberately postponed?
- How many personas and sessions need to be supported initially?
- What admin and debugging features are already required in the MVP?
- Which UI functions are mandatory, which can initially be done via API or simple debug views?
- Which technical optimizations are only implemented after measurements?

## Intended classification

### MUST

Essential for the first functional vertical slice.

Examples:

- three distinctly different personas;
- a scenario;
- an ongoing session;
- persistent event streams for genuine domain aggregates;
- a simple `FreeSociety` mode;
- Human input;
- Pause, Resume and Stop;
- fixed turn and token limits;
- vLLM serial access;
- structured and validated LLM output;
- manual step mode;
- basic selection and prompt debug information.

### SHOULD

Part of the MVP, but not necessarily part of the first vertical slice.

Examples:

- convenient persona editor;
- SSE live view;
- Session history;
- simple benchmark and evaluation functions.

### LATER

Architecturally considered but not implemented in the MVP.

Examples:

- persistent persona memory across sessions;
- second productive interaction mode, for example Committee or TalkShow;
- lasting relationship consolidation;
- RAG and embeddings;
- multiple locations;
- World Controllers;
- automatic mood and relationship evaluation;
- Branching and regeneration;
- multiple models per session.

### NOT NOW

Expressly not to be implemented.

Examples:

- Microservices;
- Message Brokers;
- Actor Framework;
- Persona Pods or Persona Processes;
- parallel persona inference;
- external KV cache manager;
- external tools or internet access through personas;
- autonomous recursive agent chains.

## Expected result

A binding scope matrix with `MUST`, `SHOULD`, `LATER` and `NOT NOW`. Each entry receives a short justification.

## Completion criteria

- [x] The central hypothesis of the first spike is formulated.
- [x] The first interaction mode is set.
- [x] The second reference mode is fixed or deliberately postponed.
- [x] All MVP features are classified.
- [x] Explicit non-targets are documented.
- [x] There are no vague formulations such as “participate in the construction if necessary”.

---

# 5. Work package 3: Aggregate, Command and Event contracts

**Status:** OPEN  
**Blocking:** Yes

## Goal

The technical concepts must be translated into clear contracts for the first implementation. Codex should not decide for itself which aggregates exist, which state transitions are permitted or which event payloads are required.

## Points to be clarified

### Aggregates and Entities

- final aggregate roots for the MVP;
- Responsibility boundaries of `Persona`, `Scenario`, `PersonaRelationship` and `Session`;
- Entities and Value Objects within the aggregates;
- References between independently event-sourced aggregates;
- Immutability of `PersonaVersion`, `ScenarioVersion` and interaction mode configuration after session start.

### Session Lifecycle

- persisted states and calculated predicates;
- Deciding whether `READY` is a real state or `CanStart` is a calculated predicate;
- permitted state transitions;
- terminal states;
- Separation of domain state and operational recovery state.

### Commands

At least to check:

```text
CreateSession
ConfigureSession
AddParticipant
RemoveParticipant
StartSession
PauseSession
ResumeSession
CompleteSession
AbortSession
SubmitHumanMessage
InjectScenarioEvent
ApplyParticipantAction
```

### Domain Events

At least to check:

```text
SessionCreated
SessionConfigured
ParticipantAdded
ParticipantRemoved
SessionStarted
SessionPaused
SessionResumed
SessionCompleted
SessionAborted
HumanSpoke
ParticipantSpoke
ScenarioEventOccurred
```

Not every suggested event type has to be accepted. Events should have technical meaning and not be artificially granular.

### Event metadata

- `EventId`;
- `SessionId` or Aggregate ID;
- Aggregate version;
- Event Sequence;
- Timestamp;
- actor;
- Correlation and causation;
- Event Schema Version;
- Model metadata, if necessary for reproducibility.

### Errors and Concurrency

- Domain Rejections;
- Expected Aggregate Version;
- Behavior for stale inference results;
- Idempotency and deduplication;
- Transaction boundary between event append and operational turn completion.

## Expected result

A compact contract document with:

- Aggregate profiles;
- Invariants;
- Command Contracts;
- Event Contracts and Payloads;
- state machines;
- Error cases;
- at least one complete example process.

## Completion criteria

- [ ] Each MVP command has purpose, inputs, preconditions and possible results.
- [ ] Each MVP event type has a specific payload.
- [ ] Aggregate invariants are explicit.
- [ ] State machines are unique.
- [ ] Domain and operational runtime state are separated.
- [ ] Concurrency and stale-result behavior are fixed.
- [ ] A complete session turn can be traced using the contracts.

---

# 6. Work package 4: Application ports and responsibility boundaries

**Status:** OPEN  
**Blocking:** Yes

## Goal

The application layer requires explicit capabilities without letting infrastructure details seep into domain or use cases. Before implementation, not all method signatures should be permanently frozen, but responsibilities, data flow and port limits should be defined.

## Components to be clarified

```text
SessionOrchestrator
IInteractionMode
IParticipantSelectionPolicy
IParticipantDecisionService
IContextBuilder
IInferenceClient
ParticipantActionValidator
Session Runtime Scheduler / Queue
Event Store
Projection Dispatcher
Read Model Queries
Live Event Publisher
Clock
Random Source
Token Counter
```

## Key questions

- Which ports are really needed by the application?
- Which concepts belong in domain, application or infrastructure?
- Which interfaces would simply be a precautionary abstraction without any concrete use?
- Which data types are allowed to pass through the port boundaries?
- How does `IInferenceClient` remain free of vLLM and OpenAI-specific technicalities?
- Where does structured output parsing occur?
- Where does the technical validation of a ParticipantAction take place?
- Who owns retry decisions?
- Who publishes SSE updates and when?
- How is persist-before-publish guaranteed?

## Expected result

A component and port overview with:

| Component | layer | Responsibility | May use | Not allowed to decide |
|---|---|---|---|---|

Minimal C#-like contract sketches should then be created for central ports. These are the design basis and not yet an implementation order.

## Completion criteria

- [ ] Each central responsibility has exactly one primary owner.
- [ ] Domain does not contain infrastructure ports.
- [ ] API remains composition root and transport facade.
- [ ] Inference, persistence, projection and live publication are separated.
- [ ] Persist-before-publish is visible in the flow.
- [ ] No unnecessary interfaces or framework abstractions were introduced.

---

# 7. Work package 5: Incremental implementation and acceptance plan

**Status:** OPEN  
**Blocking:** Yes

## Goal

The implementation is broken down into small, individually verifiable stages. Each stage should have a limited scope of changes and an explicit stop point.

## Requirements for each implementation stage

Each stage describes:

```text
Goal
fachlicher Nutzen
Voraussetzungen
In Scope
Out of Scope
affected projects and components
expected files or artifacts
automatisierte Tests
manual verification steps
architecture checks
Abnahmekriterien
Stop-Punkt
offene Folgeentscheidungen
```

## Preliminary stage sequence

1. Repository and solution structure.
2. Architecture testing for project references.
3. Review and if necessary modernize `DDD.BuildingBlocks`.
4. Common IDs, Value Objects and Error Concept.
5. `Persona` and `PersonaVersion`.
6. `Scenario` and `ScenarioVersion`.
7. `PersonaRelationship` Baseline.
8. Minimal `Session` Aggregate.
9. Session lifecycle and first domain events.
10. In-Memory Event Store and Rehydration.
11. Optimistic Concurrency and Aggregate Tests.
12. First read model projection and projection recovery.
13. Deterministic Minimal Context Builder.
14. Fake Inference Client.
15. A manually triggered participant turn.
16. Real vLLM Client.
17. Simple `FreeSociety` Selection Policy.
18. Manual Step Mode.
19. Controlled automatic runtime pulses.
20. REST and SSE.
21. Minimal web UI.
22. Vertical three-persona spike.
23. Evaluation and measurement.
24. Only then containerization, PostgreSQL and k3s deployment.

This order is a starting point. It must be reviewed after the previous work packages have been completed.

## Codex job format

Any subsequent Codex assignment should use roughly the following structure:

```text
Implement only [stage goal].

In scope:
- ...

Out of scope:
- ...

Binding architectural rules:
- ...

Acceptance criteria:
- ...

Required tests:
- ...

Document all additional decisions that become necessary.
Do not make scope-expanding architectural decisions independently.
Stop after completing this stage.
```

## Completion criteria

- [ ] Each stage creates a verifiable intermediate result.
- [ ] No stage unnecessarily mixes domain, persistence, runtime and UI.
- [ ] Each stage has explicit non-goals.
- [ ] Automated and manual acceptance are defined.
- [ ] There is a real stop point after each stage.
- [ ] Deployment does not occur before a locally checked vertical slice.

---

# 8. Work package 6: Evaluation and testing strategy

**Status:** OPEN  
**Blocking:** Partial  
**Note:** A minimal testing approach is required before implementation. The full benchmark suite can be created incrementally.

## Goal

The quality of the simulation cannot only be assessed as “feels good”. Technical correctness, persona distinctiveness and social plausibility require separate testing criteria.

## Test levels

### Architecture testing

- permitted project references;
- Domain without ASP.NET Core, EF Core, HTTP or vLLM;
- API without business logic;
- Infrastructure implements application ports.

### Domain testing

- Aggregate Invariants;
- State Transitions;
- Given-When-Then event sourcing tests;
- deterministic `Apply`;
- rehydration;
- Snapshot equivalence.

### Application testing

- Orchestration priorities;
- Human Priority;
- Actor Eligibility;
- stale-turn protection;
- `DoNothing` and Idle Handling;
- persist-before-publish;
- Retry behavior.

### Projection Tests

- idempotent projection;
- Recovery after errors;
- incremental reprojection;
- complete rebuild;
- Behavior over process restarts.

### Context Builder Tests

- deterministic serialization;
- stable block order;
- identical event representation;
- Token budgeting;
- correct separation of stable and dynamic blocks;
- Reconstruction after application or vLLM restart.

### Behavioral Evaluation

- Persona Identity Test;
- Persona Classification Test;
- Single Trait Variation Test;
- Relationship Test;
- Context Persistence Test;
- Persona Collapse Detection;
- Multi-Situation Evaluation;
- Side-by-side comparison of Persona versions;
- Human Evaluation;
- LLM-as-Judge only as a supplement.

### Runtime and inference benchmarks

- Prompt tokens;
- Cached Prompt Tokens;
- Cache Hit Ratio;
- Prefill latency;
- Time to First Token;
- Decode Rate;
- Total duration;
- `A-B-C-A` and `A-B-C-D-E-F-G-H-I-J-A`;
- Cold cache versus stable prefix cache;
- Context sizes from small to large.

## Quality dimensions

The evaluation should not necessarily produce a single overall score. Relevant dimensions are:

- Persona distinctiveness;
- consistency;
- Contextual fidelity;
- relationship sensitivity;
- Quality of interaction;
- ability to deal with conflict;
- scenario fidelity;
- entertainment value;
- Human controllability;
- limitability;
- Latency and resource consumption.

## Expected result

An executable test and evaluation plan with fixtures, metrics, test data and an initial regression suite.

## Completion criteria

- [ ] Technical correctness and behavioral quality are separate.
- [ ] The central quality risks have concrete tests.
- [ ] Seeded evaluation is intended for deterministic orchestration.
- [ ] Model variance is taken into account during interpretation.
- [ ] LLM-as-Judge is not the only assessment authority.
- [ ] The first vertical spike has defined success criteria before its implementation.

---

# 9. Work package 7: Frontend and operating concept for the MVP

**Status:** OPEN  
**Blocking:** For backend start no, for vertical MVP yes

## Goal

The UI should not only enable administration, but also make the simulation observable, controllable and understandable. The specific frontend technology and the minimum range of functions have yet to be determined.

## Points to be clarified

- Frontend technology;
- separate frontend or ASP.NET integrated approach;
- Persona Editor;
- Scenario Editor;
- Relationship Editor;
- Session configuration;
- Live Event Feed;
- Human Message and Scenario Event Injection;
- Pause, Resume, Stop and Step;
- Display of runtime and domain state;
- Selection Trace and Context Debug View;
- Prompt inspection without disclosing internal chain-of-thought;
- Handling temporary token streaming versus persistent events;
- Display of projection lag or runtime errors.

## Recommended MVP priority

The first UI should primarily enable:

```text
select/start session
Eventstream beobachten
Human Input senden
Scenario Event injizieren
Pause / Resume / Step / Stop
Selection Trace ansehen
inspect the generated prompt or context blocks
```

A fully convenient persona and relationship editor can follow, provided simple fixtures or minimal administration are sufficient for the spike.

## Expected result

- Decision on front-end technology;
- small list of MVP screens;
- Description of the most important user flows;
- API and SSE requirements from UI perspective;
- Differentiation between user view and admin/debug view.

## Completion criteria

- [ ] Frontend technology is decided.
- [ ] MVP screens and user flows are defined.
- [ ] Step Mode and Human Intervention can be operated.
- [ ] Persistent events and temporary streaming are visually separated.
- [ ] Debugging functions are distinguishable from normal user functions.
- [ ] The UI does not pull any new technical logic into the client.

---

# 10. Work package 8: Review of DDD.BuildingBlocks

**Status:** OPEN  
**Blocking:** Yes, before event sourcing infrastructure is implemented

## Goal

The existing framework `olivergrau/ddd-building-blocks` is intended to serve as the basis for DDD, event sourcing, snapshots and projections. Before using it, the actual current status of the repository must be checked.

## Scope of testing

- current .NET compatibility;
- Build and test status;
- Nullable Reference Types;
- Async and cancellation support;
- Aggregate and event abstractions;
- Event Store Providers;
- Snapshot mechanism;
- Optimistic Concurrency;
- Projection Infrastructure;
- Projection checkpoints and recovery;
- Dependency injection integration;
- EF Core or Persistence compatibility;
- Linux and container compatibility;
- Extensibility for SQLite and PostgreSQL;
- Framework dependencies and obsolete packages;
- existing tests and identified coverage gaps;
- possible violations of the desired dependency direction.

## Decision options

### A. Use directly

The framework sufficiently meets the requirements.

### B. Modernize in a targeted manner

The semantics are retained, technical parts are updated and tested.

### C. Use partially

Only clearly suitable building blocks are adopted. Missing functions are added outside the framework.

### D. Do not use

Only if there are concrete technical or architectural reasons against its use. A new development of the event sourcing infrastructure is not an automatic standard decision.

## Expected result

A review report with:

- Repository and project overview;
- Capability Matrix against Playground requirements;
- technical risks found;
- necessary modernizations;
- test evidence;
- clear usage decision;
- if necessary, your own small modernization plan.

## Completion criteria

- [ ] The framework has been checked against actual code.
- [ ] Build and tests were carried out and blockers were documented.
- [ ] Capability gaps are known.
- [ ] Modernization needs and scope are limited.
- [ ] There is a justified Use/Adapt/Reject decision.
- [ ] No parallel second event sourcing infrastructure will be started.

---

#11. Recommended editing order

The work packages should be processed in the following order:

```text
1. Document hierarchy and decision status
2. Verbindlicher MVP-Scope
3. Aggregate, command, and event contracts
4. Application ports and responsibility boundaries
5. Evaluation and minimum success criteria
6. Review DDD.BuildingBlocks
7. Frontend and interaction concept
8. Finalize the incremental implementation and acceptance plan
```

The DDD.BuildingBlocks review can partly be prepared in parallel with the contract work. However, its results must not tacitly determine the domain model.

The frontend decision does not block the first domain stages. However, it must be completed before planning the full vertical MVP.

---

#12. Readiness gate before first implementation

The first code stage may begin if at least the following conditions are met:

- [ ] Document hierarchy and current architectural decisions are consolidated.
- [ ] The scope of the first implementation slice is unique.
- [ ] The aggregates, commands and events required for this slice are defined.
- [ ] The relevant layer boundaries and ports have been clarified.
- [ ] Acceptance criteria and tests for the slice have been determined.
- [ ] The Codex order has explicit in-scope and out-of-scope boundaries.
- [ ] The order ends with a review and stop point.

Not all work packages have to be completed down to the last detail. However, it must be prevented that the first slice anticipates decisions that are still consciously open.

---

# 13. Definition of Done of the preparation phase

The entire preparatory phase is completed when:

- the current specification status can be navigated without contradictions;
- the MVP is clearly differentiated from later ideas;
- Domain and event contracts for the MVP are available;
- Application and infrastructure boundaries are clarified;
- the use of DDD.BuildingBlocks has been technically evaluated;
- UI and human control are defined for the MVP;
- Evaluation and regression are planned from the start;
- the implementation is broken down into small, individually removable Codex stages;
- There is no longer an open point that Codex would inevitably and uncontrollably have to decide itself when implementing.

Then the implementation doesn't start with:

> Build the Persona Simulation Playground.

But with a small, verifiable order, the result of which can be fully understood and accepted.

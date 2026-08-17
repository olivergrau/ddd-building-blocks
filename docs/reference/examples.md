# Example applications

The repository contains two complementary examples. Use them as executable references, not as universal domain templates.

## RocketLaunch

RocketLaunch demonstrates an application/API/read-model slice.

### Useful locations

| Topic | Location |
|---|---|
| aggregate behavior | `example/RocketLaunch.Domain/Model/Mission.cs`, `CrewMember.cs` |
| value objects | `example/RocketLaunch.SharedKernel/ValueObjects/` |
| persisted events | `example/RocketLaunch.SharedKernel/Events/` |
| event registry/codec | `example/RocketLaunch.SharedKernel/Events/RocketLaunchEventCodec.cs` |
| snapshot and registry | `example/RocketLaunch.SharedKernel/Snapshots/` |
| commands/handlers | `example/RocketLaunch.Application/Command/` |
| composition root | `example/RocketLaunch.Api/Program.cs` |
| read-model projectors | `example/RocketLaunch.ReadModel.Core/Projector/` |
| domain/application tests | `example/RocketLaunch.Domain.Tests/`, `RocketLaunch.Application.Tests/` |

### What to learn

- creation and lifecycle events;
- target-version usage;
- external availability ports passed into domain behavior;
- aggregate relations by identity;
- thin command handlers and aggregate sourcing;
- stable event/snapshot registration;
- snapshot implementation for a nontrivial aggregate;
- scoped dispatch registration.

### Limitation

The API example still uses its historical asynchronous in-memory publishing table for its read model. Its API integration tests have a known timing race. Use the framework's `ProjectionRunner` and checkpoint stores for new recoverable projection implementations.

## LunarOps

LunarOps emphasizes domain modeling across multiple aggregate lifecycles and domain services.

### Useful locations

| Topic | Location |
|---|---|
| mission aggregate | `example/LunarOps.Domain/Model/LunarMission.cs` |
| station aggregate | `example/LunarOps.Domain/Model/MoonStation.cs` |
| internal entities | `example/LunarOps.Domain/Model/Entities/` |
| domain services | `example/LunarOps.Domain/Service/` |
| events | `example/LunarOps.SharedKernel/Events/` |
| lifecycle integration test | `example/LunarOps.Domain.Tests/Application/MissionLifecycleIntegrationTests.cs` |

### What to learn

- choosing separate aggregate consistency boundaries;
- coordinating docking, payload, crew, and undocking rules through services;
- keeping value objects and events in a shared domain contract assembly;
- testing multi-step domain workflows without relational aggregate persistence.

## Reading order

1. Read the aggregate's public business methods.
2. Read the events raised by each method.
3. Read private event handlers to see state transitions.
4. Read domain tests for accepted/rejected behavior.
5. Read application handlers and composition root.
6. Compare read-model projectors with aggregate state; note that their purpose differs.

## 🌑 LunarOps Domain – Formalized Design Overview

### 🧩 **Aggregate Roots**

---

### 1. **LunarMission (Aggregate Root)**

**Represents a vehicle's lifecycle after arriving in lunar orbit.**

#### ✅ Properties

* `ExternalMissionId` (identity)
* `ArrivalTime`
* `VehicleType` (string or enum)
* `Status: LunarMissionStatus` (`Registered`, `DockingScheduled`, `Docked`, `Unloaded`, `InService`, `Departed`)
* `PayloadManifest: IReadOnlyCollection<LunarPayload>` (ValueObjects)
* `CrewManifest: IReadOnlyCollection<LunarCrewMember>` (Entities)
* `AssignedStationId: StationId?`
* `AssignedDockingPortId: DockingPortId?`

#### ✅ Domain Rules

* Must reference a valid MoonStation at registration.
* At registration:

    * VehicleType must be supported by MoonStation.
    * MoonStation must have **sufficient crew capacity** for the crew manifest.
    * Payload capacity is not checked yet.
* `ScheduleDocking` can only be called after registration.
* `UnloadCrew` and `UnloadPayload` can only occur after docking.
* Mission transitions to `InService` only when both crew and payload are unloaded.
* Departure is only allowed from the `InService` state.

---

### 2. **MoonStation (Aggregate Root)**

**Owns physical station state: docking ports, crew quarters, payload storage.**

#### ✅ Properties

* `StationId` (identity)
* `Name`
* `Location` (e.g., South Pole)
* `SupportedVehicleTypes: IReadOnlyCollection<string>`
* `DockingPorts: List<DockingPort>` (Entities)
* `MaxCrewCapacity: int`
* `CrewQuarters: List<LunarCrewMember>` (current occupants)
* `MaxPayloadCapacity: double` (e.g., max total mass)
* `StoredPayloads: List<LunarPayload>` (currently stored payloads)
* `OperationalStatus: StationStatus` (e.g., Active, Maintenance, Emergency)

#### ✅ Domain Rules

* Docking port must be **available and unoccupied** to be reserved.
* Crew may only be assigned if **crew quarters have space**.
* Payload may only be stored if **capacity is sufficient**.
* DockingPort must be explicitly **released** on departure.
* Lifecycle changes (crew assignment, port reservation) are part of the aggregate’s consistency boundary.

---

### 🧠 **Supporting Entities / Value Objects**

* `LunarCrewMember` (Entity): `Name`, `Role`, `Status`
* `LunarPayload` (Value Object): `PayloadId`, `Type`, `Mass`, `DestinationArea`
* `DockingPort` (Entity): `PortId`, `OccupancyStatus`, `AssignedMissionId`

---

## 🔁 **Lifecycle Flow**

---

### 1. **Mission Registration (Triggered by `MissionArrivedAtLunarOrbit`)**

* Create new `LunarMission` with crew and payload manifest.
* Check if:

    * MoonStation supports the given `VehicleType`
    * Crew capacity at the station is sufficient
* If valid: set status `Registered`, reference station

✅ Done entirely in `LunarMission` + validation via `DockingCoordinator` service

---

### 2. **Schedule Docking**

* MoonStation:

    * Checks docking port availability
    * Confirms storage capacity for payload
    * Confirms crew capacity again
* If valid:

    * Reserve port (`DockingPort.ReserveFor(missionId)`)
    * Update available port count (implicitly or via state)
    * `LunarMission.AssignPort(portId)`
    * Status → `DockingScheduled`

✅ Done via `DockingCoordinator` domain service

---

### 3. **Complete Docking**

* Called when physical docking occurs
* `LunarMission`:

    * Status → `Docked`
* `MoonStation`:

    * No-op unless you want to mark port as physically connected

---

### 4. **Unload Payload**

* Only allowed if mission is `Docked`
* Payload transferred to MoonStation
* MoonStation:

    * Validates storage capacity (again)
    * Adds `LunarPayload` to storage
* Partial success possible (if unloading is granular)

---

### 5. **Transfer Crew**

* Only allowed if mission is `Docked`
* Crew transferred to MoonStation
* MoonStation:

    * Validates crew quarters capacity again
    * Adds crew to crew quarters

---

### 6. **Mark In-Service**

* Only allowed when **both**:

    * All payload is unloaded
    * All crew is transferred
* `LunarMission.Status → InService`

---

### 7. **Departure**

* Only allowed when mission is `InService`
* `MoonStation`:

    * Releases docking port
    * Optionally clears payload/crew references if returning
* `LunarMission.Status → Departed`

---

## 🔐 Constraints Summary (Enforced by Domain Services or Aggregates)

| Rule                                        | Enforced By                            |
| ------------------------------------------- | -------------------------------------- |
| Vehicle type must be supported              | `DockingCoordinator` via `MoonStation` |
| Sufficient crew capacity at registration    | `DockingCoordinator`                   |
| Sufficient storage at docking               | `DockingCoordinator`                   |
| Payload/crew can only be unloaded if docked | `LunarMission` methods                 |
| Departure only from `InService`             | `LunarMission`                         |

---
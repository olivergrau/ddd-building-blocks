# 🌑 Lunar Operations – Domain Definition (Business Language)

---

## 🎯 Core Purpose

To manage all mission activities **after a vehicle has arrived in lunar orbit or at a Moon Station**. This includes docking logistics, crew disembarkation, payload handling, station resource management, and mission departure coordination. The context begins its responsibility when a mission is handed over via the `MissionArrivedAtLunarOrbit` event.

---

## 🧩 Core Domain Concepts

---

### 1. **Lunar Mission**

A lunar mission represents the arrival of a vehicle from Earth and the associated operations at or near a Moon Station.

* **Attributes**:

  * External Mission ID (from Rocket Launch)
  * Arrival Time
  * Vehicle Type (e.g., Starship, Orion Capsule)
  * Docking Status: Awaiting, Docked, Departed
  * Crew Manifest
  * Payload Manifest
  * Assigned Moon Station (if docked)

* **Rules**:

  * A lunar mission is created by reacting to the `MissionArrivedAtLunarOrbit` event.
  * Crew and payload must be fully registered before they can be transferred.
  * Docking must be completed before further operations (crew/payload transfer).

---

### 2. **Moon Station**

A permanent or semi-permanent facility for handling arriving missions.

* **Attributes**:

  * Station ID / Name
  * Location (e.g., Lunar South Pole, Equatorial Orbit)
  * Supported Vehicle Types
  * Docking Ports (count and availability)
  * Crew Capacity (max concurrent headcount)
  * Operational Status: Active, Maintenance, Emergency

* **Rules**:

  * A station can only dock as many vehicles as it has available ports.
  * Each docking port may be reserved or blocked (e.g., for emergencies).
  * A station must have room for the arriving mission’s crew and payload.

---

### 3. **Docking Port**

A dedicated interface for docking an arriving vehicle.

* **Attributes**:

  * Port ID
  * Occupancy Status
  * Assigned Vehicle
  * Linked Lunar Mission (if docked)

* **Rules**:

  * Only one vehicle can occupy a port at a time.
  * Port must be released before a new mission can dock.

---

### 4. **Lunar Crew Member**

A crew member present on or transferred to the Moon via a lunar mission.

* **Attributes**:

  * Name, Role (e.g., Commander, Scientist)
  * Source Mission
  * Current Assignment: Active, Off-Duty, Returned

* **Rules**:

  * Crew must be listed in the mission’s manifest to be transferred.
  * The station must have sufficient life support and crew capacity.
  * Crew may be re-assigned or rotated as part of future operations.

---

### 5. **Lunar Payload**

Cargo delivered to the Moon for scientific, infrastructural, or logistic purposes.

* **Attributes**:

  * Payload ID
  * Description / Type (e.g., Rover, Supplies, Equipment)
  * Mass
  * Destination Area (e.g., Storage, Science Lab)

* **Rules**:

  * Payload may only be unloaded after docking is complete.
  * Storage or usage location must support the item type and volume.
  * Oversized or hazardous payloads may require approval.

---

## 🔁 Integration Event: `MissionArrivedAtLunarOrbit`

> **Source**: Rocket Launch Scheduling System
> **Purpose**: Signals that a mission has arrived in lunar orbit and is ready to initiate docking and lunar operations.

### Reaction:

* A new **Lunar Mission** is created based on this event.
* The crew and payload manifest are recorded.
* A docking port is scheduled or reserved (if available).
* Lunar Ops begins its responsibility for vehicle, crew, and cargo.

---

## 📅 Lunar Mission Lifecycle

| Phase            | Description                                        |
| ---------------- | -------------------------------------------------- |
| Registered       | Mission received from Rocket Launch via event      |
| DockingScheduled | Docking port assigned at a Moon Station            |
| Docked           | Vehicle successfully docked                        |
| Unloaded         | Payload and crew have disembarked                  |
| InService        | Crew is active on Moon; mission operations ongoing |
| Departed         | Vehicle has left the station                       |

---

## 🧭 Use Cases / Scenarios

---

### 1. **Register Lunar Mission**

* Triggered by: `MissionArrivedAtLunarOrbit` event
* Outcome: Lunar mission entity is created and queued for docking

---

### 2. **Assign Docking Port**

* Preconditions: Station has an available, compatible port
* Outcome: Mission assigned to a docking slot

---

### 3. **Complete Docking**

* Preconditions: Vehicle has physically connected to the port
* Outcome: Docking status updated, operations may proceed

---

### 4. **Transfer Crew to Station**

* Preconditions: Docking complete, crew capacity available
* Outcome: Crew members are marked as active on station

---

### 5. **Unload Payload**

* Preconditions: Docking complete, storage area available
* Outcome: Payload items are transferred to their intended destination

---

### 6. **Handle Departure**

* Preconditions: Mission is marked for return or transfer
* Outcome: Vehicle undocks, docking port becomes available

---

## 🔐 Constraints & Invariants

| Constraint                               | Explanation                                                        |
| ---------------------------------------- | ------------------------------------------------------------------ |
| One vehicle per docking port             | Avoids resource conflicts                                          |
| Station capacity enforcement             | Crew and payload must fit within life support and storage capacity |
| Event-driven initiation                  | Only creates lunar missions via `MissionArrivedAtLunarOrbit`       |
| No crew/payload transfers before docking | Enforces procedural correctness                                    |

---

## 📦 Summary

The **Lunar Operations** bounded context manages all activities following a mission's arrival in lunar orbit. It starts its lifecycle upon receiving a `MissionArrivedAtLunarOrbit` integration event from the Rocket Launch context. From that point forward, it owns the full process of docking, offloading, crew assignment, and eventual vehicle departure. This context is designed to be event-driven, autonomous, and cleanly separated from upstream responsibilities.

---
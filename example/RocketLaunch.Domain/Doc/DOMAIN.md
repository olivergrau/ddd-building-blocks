# 🚀 Rocket Launch Scheduling System – Domain Definition (Business Language)

---

## 🎯 Core Purpose

To plan, schedule, and coordinate rocket missions from Earth to space, ensuring launch windows, vehicle readiness, launch pad availability, crew assignments, and successful mission liftoff. Once the mission reaches lunar orbit, the system emits an event to notify downstream systems (e.g., Lunar Operations) of its arrival.

---

## 🧩 Core Domain Concepts

---

### 1. **Mission**

A mission represents the goal of a rocket launch — such as delivering a payload and/or crew to lunar orbit — within a defined launch window.

* **Attributes**:

  * Mission Name
  * Target Orbit
  * Payload Description
  * Launch Window (Start and End Time)
  * Assigned Rocket
  * Assigned Launch Pad
  * Assigned Crew Members
  * Mission Status: Planned, Scheduled, Launched, Arrived, Aborted, Completed

* **Rules**:

  * A mission cannot be scheduled unless a rocket, launch pad, and required crew (if crewed) are assigned.
  * The payload must not exceed the rocket’s capacity.
  * A mission can be **crewed** or **uncrewed**.
  * Missions can be aborted before launch.
  * Once launched and arrived at lunar orbit, the mission emits an integration event.

---

### 2. **Rocket**

A reusable or expendable launch vehicle used to carry crew and/or cargo into space.

* **Attributes**:

  * Rocket ID / Name
  * Thrust Capacity
  * Payload Capacity (mass)
  * Crew Capacity (maximum crew size)
  * Status: Available, Assigned, Under Maintenance

* **Rules**:

  * A rocket can only be assigned to one mission at a time.
  * It must be available and meet the mission’s payload and crew requirements.

---

### 3. **Launch Pad**

A physical launch site where a rocket is prepared and launched.

* **Attributes**:

  * Pad ID / Name
  * Location
  * Supported Rocket Types
  * Status: Available, Occupied, Under Maintenance

* **Rules**:

  * A pad can only be used by one mission at a time.
  * It must be available for the entire launch window.

---

### 4. **Crew Member**

A person assigned to participate in a mission in a specific operational role.

* **Attributes**:

  * Crew Member ID / Name
  * Role: Commander, Pilot, Mission Specialist, Flight Engineer, etc.
  * Certification Level(s)
  * Status: Available, Assigned, Unavailable

* **Rules**:

  * Only certified and available crew members can be assigned.
  * Certain roles (e.g. Commander, Pilot) may be mandatory for crewed missions.
  * Crew members may only participate in one mission at a time.
  * Uncrewed missions do not require crew assignments.

---

## 📅 Mission Lifecycle

| Phase       | Description                                                         |
| ----------- | ------------------------------------------------------------------- |
| Planned     | Mission is defined but not yet assigned resources                   |
| Scheduled   | Rocket, pad, and crew are assigned and the mission is ready to go   |
| Launched    | The mission has been launched successfully                          |
| **Arrived** | The vehicle has reached lunar orbit and triggered external handover |
| Completed   | The mission is formally closed (after return or arrival)            |
| Aborted     | The mission was cancelled before launch                             |

---

## 🔁 Integration Event: `MissionArrivedAtLunarOrbit`

> **Purpose:** To signal to external contexts (e.g., Lunar Operations) that a mission has reached its destination and is ready for post-arrival procedures.

### Emitted When:

* A mission that was in the “Launched” state has completed its transit and is confirmed to be in lunar orbit.

### Event Contents:

* Mission ID
* Arrival Time
* Vehicle Type (e.g., Starship, Orion)
* Crew Manifest (name and role per crew member)
* Payload Manifest (items and mass)

### Behavior:

* This event marks the **handover of responsibility** from Rocket Launch to Lunar Operations.
* The Rocket Launch context does **not depend** on how the downstream context reacts.

---

## 🧭 Use Cases / Scenarios

---

### 1. **Create a New Mission**

* Input: Mission name, target orbit, payload description, desired launch window
* Outcome: Mission is registered in the “Planned” state

---

### 2. **Assign a Rocket to the Mission**

* Preconditions: Rocket is available and has sufficient capacity
* Outcome: Rocket is linked to mission

---

### 3. **Assign a Launch Pad**

* Preconditions: Pad is available during the mission’s window
* Outcome: Pad is linked to mission

---

### 4. **Assign Crew Members**

* Preconditions:

  * Mission is crewed
  * Required roles are specified
  * Crew members are certified and available
* Outcome: Crew is assigned to the mission

---

### 5. **Schedule the Mission**

* Preconditions:

  * Rocket, pad, and required crew are assigned
* Outcome: Mission moves to “Scheduled” state

---

### 6. **Abort the Mission**

* Preconditions: Mission is not yet launched
* Outcome: Status is set to “Aborted”

---

### 7. **Mark Mission as Launched**

* Preconditions: Mission is scheduled, all checks passed
* Outcome: Mission status becomes “Launched”

---

### 8. **Mark Mission as Arrived in Lunar Orbit**

* Preconditions: Mission is in “Launched” state
* Outcome:

  * Mission moves to “Arrived” or “Completed” state
  * `MissionArrivedAtLunarOrbit` integration event is emitted

---

## 🔐 Constraints & Invariants

| Constraint                     | Explanation                                           |
| ------------------------------ | ----------------------------------------------------- |
| No overlapping pad assignments | One mission per pad per time window                   |
| Rocket exclusivity             | One rocket per mission                                |
| Payload and crew capacity      | Must not exceed rocket capabilities                   |
| Required crew roles            | Commander/Pilot roles mandatory for crewed missions   |
| No crew overlap                | Crew can only be assigned to one mission at a time    |
| Mission arrival triggers event | External systems rely on `MissionArrivedAtLunarOrbit` |

---

## 📦 Summary

The **Rocket Launch Scheduling System** is responsible for the full lifecycle of Earth-originated space missions — from planning through launch and lunar arrival. It owns the domain model of missions, rockets, launch pads, and crew coordination. When a mission completes its transfer to lunar orbit, it emits a formal integration event to trigger workflows in external bounded contexts like **Lunar Operations**.

This event-driven architecture ensures **clean decoupling**, while supporting a rich and operationally realistic domain model.
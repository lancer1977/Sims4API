# Stream Influence Events

> DreadBreadcrumb brainstorm and card pack for audience-driven Sims incidents.

**Goal:** Let stream viewers trigger approved in-game influence events that create visible chaos, story beats, and guest pressure without turning the mod into an unbounded spawn machine.

**Scope:** This is the game-side influence layer, not the transport plumbing. The existing shared contracts, queue reader, and bridge runtime stay the foundation.

---

## Brainstorm: summon / influence ideas

These are the event families that felt like the right fit from the user prompt:

- **Fire incident**
  - kitchen fire
  - appliance fire
  - smoke alarm / evacuation beat
  - firefighter arrival
- **Robber / break-in**
  - burglar or masked robber visit
  - alarm-triggered police response
  - stolen-object pressure
  - recovery / aftermath beat
- **Other guests / surprise visitors**
  - uninvited guest
  - surprise friend or rival
  - awkward neighbor drop-in
  - celebrity / fan / random social pressure guest

Possible later extensions, if the core three land well:

- repair crew / plumbing disaster
- loud neighbor / complaint visit
- prank / party-crasher guest
- special-event visitor with an approval gate

---

## Card pack

### 1) Define the stream influence contract and safety gates

**Objective:** Add the event taxonomy, cooldowns, approval rules, and audit trail for stream-triggered influence events.

**Files:**
- Modify: `Sims4.Core/SimsBridgeContracts.cs`
- Modify: `Sims4.Core/SimsModRuntime.cs`
- Modify: `docs/contracts/json-wire-format.md`
- Modify: `README.md`
- Modify: `docs/roadmap/v1/README.md`

**Acceptance criteria:**
- influence event names are canonical and documented
- unsafe or duplicate triggers are rejected or cooled down
- the mod can log which influence event fired and why
- docs describe the allowed event surface and explicit out-of-scope cases

---

### 2) Implement the fire incident influence path

**Objective:** Turn a fire summon into a concrete in-game incident path with clear outcome reporting.

**Files:**
- Modify: `Sims4.Core/SimsModRuntime.cs`
- Modify: `Sims4.Core/SimsCommandDispatching.cs`
- Add or modify tests under `Tests/PolyhydraGames.Sims4.Tests/`
- Update: `docs/roadmap/v1/stream-influence-events.md`

**Acceptance criteria:**
- a fire influence command can be queued and recognized
- the runtime records the fire incident outcome in the status journal
- the behavior is covered by tests for both happy-path and invalid input
- the doc states what counts as “fire” versus general chaos

---

### 3) Implement the robber / break-in influence path

**Objective:** Add the robbery/break-in summon as a distinct incident family with its own outcome and audit behavior.

**Files:**
- Modify: `Sims4.Core/SimsModRuntime.cs`
- Modify: `Sims4.Core/SimsBridgeContracts.cs`
- Add or modify tests under `Tests/PolyhydraGames.Sims4.Tests/`
- Update: `docs/contracts/dispatch-status.md`

**Acceptance criteria:**
- robber/break-in is a separate canonical event family
- the runtime can report success, rejection, or fallback behavior
- the status journal captures the outcome in a human-readable way
- the test suite proves the event is not conflated with fire or guest summons

---

### 4) Implement the guest / surprise visitor influence path

**Objective:** Add the “other guests” summon path for surprise visitors, awkward drop-ins, and other social pressure events.

**Files:**
- Modify: `Sims4.Core/SimsModRuntime.cs`
- Modify: `Sims4.Core/SimsBridgeContracts.cs`
- Add or modify tests under `Tests/PolyhydraGames.Sims4.Tests/`
- Update: `README.md`

**Acceptance criteria:**
- guest summons are modeled separately from fire and robber incidents
- the event can express variants such as friend, rival, neighbor, or random guest
- the runtime publishes the resulting event/status cleanly
- docs explain the user-facing behavior and the allowed variants

---

### 5) Add stream influence smoke coverage and docs closeout

**Objective:** Prove the new influence events are wired end-to-end enough for repo-level validation and future game-side work.

**Files:**
- Add or modify tests under `Tests/PolyhydraGames.Sims4.Tests/`
- Modify: `docs/roadmap/v1/README.md`
- Modify: `00_agile/planning/Api.Sims4-KANBAN.md`
- Modify: `README.md`

**Acceptance criteria:**
- there is at least one smoke-style test or harness for influence dispatch shape
- the roadmap reflects the completed/remaining influence slices
- the tracker shows the new follow-up goals clearly
- the repo notes point to the canonical influence-event doc

---

## Suggested implementation order

1. contract + safety gates
2. fire incident
3. robber incident
4. guest / surprise visitor incident
5. smoke coverage + docs closeout

## Non-goals for this pack

- No free-form event spawning without a whitelist
- No live UI changes outside the influence contract surface
- No attempt to model every Sims event type at once
- No bridge transport rewrite; the existing SignalR + queue foundation stays the same

# Sims4 Support Roadmap (v1)

## Shared phase model

This repo uses `V1` as the base contract/bridge baseline. When comparing across projects, use the common `PolyhydraGames.GameServerInterop` ladder:

- `V1` - admin control plane baseline
- `V2` - GM / moderation mode
- `V3` - cc-sidecar baseline
- `V4` - ChannelCheevos integration

The Sims roadmap below is the V1 foundation for the shared contract and bridge runtime.

## Vision
Make the Sims 4 integration a clean, testable contract layer with a small bridge runtime, so the game-side mod can expose stable capabilities without the rest of the stack guessing at wire shapes.

## Current status
- Shared event contract exists (`StreamEvent`).
- Canonical action names now include interaction/object actions (`run_interaction`, `spawn_object`) plus inventory-focused actions (`add_item`, `take_item`, `take_stock`).
- Base-mod exposure contracts exist (`SimsModCapabilities`, `SimsInventorySnapshot`, `SimsInventoryItem`).
- SignalR bridge publishes events with retry + local buffer fallback.
- Contract serialization tests and JSON wire-format docs are in place and passing.
- Live follow-up cards remain for the queue dispatcher/status-persistence subtree (`t_9ce8fcd2`, `t_f7acf1c6`, `t_bdacef85`, `t_36f03fe1`).
- Suggested execution order: schema first, dispatcher second, tests third, then root-card closeout.

## Phase backlog

### Phase 1 — contract hardening
- [x] Define the shared event envelope.
- [x] Define the canonical action/event name catalogs.
- [x] Add inventory and capability exposure models.
- [x] Add bridge runtime options for config + buffering.
- [x] Add explicit serialization round-trip tests for each contract shape.
- [x] Document the final JSON schema for command files and event payloads.

### Phase 2 — game-side base mod exposure
- [x] Define the command dispatch status journal format and dispatcher foundation.
- [x] Add the mod-side dispatcher that reads commands from the moddata queue.
- [x] Implement `add_item` and `take_item` handlers.
- [x] Implement `take_stock` inventory snapshot generation.
- [x] Publish capability handshakes on startup.
- [x] Persist processed/failed command status for troubleshooting.

### Phase 3 — smoke + test system
- [x] Add a tiny offline test harness for the command schema.
- [x] Add round-trip tests for bridge buffering and command serialization.
- [x] Add a repo-level smoke check that verifies the bridge can boot with sample config.
- [x] Add docs for local secret/config setup.

### Phase 4 — stream influence events
- [x] Define the influence event catalog and safety gates.
- [x] Implement the fire incident influence path.
- [x] Implement the robber / break-in influence path.
- [x] Implement the guest / surprise visitor influence path.
- [x] Add smoke coverage and docs closeout for stream influence events.

## Quality gate
A slice is considered complete when:
- the docs match the code,
- the contract tests pass,
- the bridge still builds,
- and any new action surface is named in the canonical catalogs.

## Known gaps
- Inventory and world-action handlers here are shared-runtime abstractions, not the shipped game mod.
- The local buffer is a fallback path, not a full durable queue.

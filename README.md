# Sims4 Support

Contracts and bridge runtime for the Polyhydra Games Sims 4 support home.

Goal shape: canonical support home for Sims contracts and bridge runtime, while the base mod remains separate from the support boundary.

This repository is the shared boundary for the Sims workflow and the canonical Sims4 support home. It is **not** the base mod itself; instead it defines the wire contracts and the small SignalR bridge that the mod can use to report state back to the hub.

## Tags

- api
- api-sims4
- dotnet
- sims4
- streaming
- signalr

## What lives here

- **`Sims4.Core`** — shared wire contracts
  - `StreamEvent` for outbound mod-to-hub events
  - `SimsCommand` for inbound action requests
  - `SimsTarget`, `SimsInventoryItem`, `SimsInventorySnapshot`
  - `SimsRunInteractionRequest`, `SimsSpawnObjectRequest`, `SimsWorldActionRecord`
  - `SimsInfluenceRequest` plus specialized guest/fire/robber influence payloads and safety-gate helpers
  - `SimsModCapabilities` for base-mod exposure
  - canonical action/event name catalogs
- **`Sims4.SignalR`** — the runtime bridge
  - connects the mod runtime to the SignalR hub
  - publishes events with retry + local buffer fallback
  - responds to heartbeat requests
- **`Tests`** — contract tests for the shared models

## Canonical action surface

The current action catalog is intentionally small and explicit:

- `add_funds`
- `add_buff`
- `send_notification`
- `run_interaction`
- `spawn_object`
- `add_item`
- `take_item`
- `take_stock`
- `trigger_influence`

The inventory-oriented actions and direct interaction/object actions are the current foundation for the base mod exposure work, and `trigger_influence` is the stream-facing entry point for audience-driven incidents.

## Canonical event surface

- `Heartbeat`
- `Capabilities`
- `InventorySnapshot`
- `CommandQueued`
- `CommandCompleted`
- `CommandFailed`

## Runtime shape

1. The mod/bridge loads configuration.
2. The SignalR connection is established with the streamer/web key.
3. Heartbeat requests can be answered immediately.
4. Capability and inventory snapshots are published as structured `StreamEvent` payloads.
5. The startup handshake publishes a timestamped capability snapshot before the bridge begins normal work.
6. The mod-side queue reader claims JSONL command batches atomically, drains them into inventory handlers, and mirrors status journals.
7. Failed sends are retried and then buffered to a local `*.jsonl` file.
8. Command dispatch outcomes can be mirrored into a local `command-status.jsonl` journal for troubleshooting.

## Configuration

The bridge reads these settings from `appsettings.json` or environment variables prefixed with `SIMS4_`:

- `HubUrl`
- `WebKey`
- `EventBufferPath` (defaults to `event-buffer.jsonl`)
- `RetryAttempts` (defaults to `5`)
- `RetryDelayMilliseconds` (defaults to `500`)

Sample local config and env-var setup lives in `docs/operations/local-config.md`.

## Roadmap

See `docs/roadmap/v1/README.md` for the current phased backlog and quality closeout checklist.
The stream-influence brainstorm and card pack live in `docs/roadmap/v1/stream-influence-events.md`.
The canonical influence-event contract lives in `docs/contracts/influence-events.md`.

## Work tracking

- [Live Kanban tracker](docs/roadmaps/planning/Sims4-Support-KANBAN.md) — mirrors the current Sims integration cards and their parked status.

## Documentation

- [Docs README](./docs/README.md)

## Notes

- Configuration is loaded from `appsettings.json` and environment variables prefixed with `SIMS4_`.
- The repo currently defines the contracts and bridge runtime; the actual game-side mod handlers can evolve against these contracts without changing the wire shape.
- JSON wire-format details live in `docs/contracts/json-wire-format.md`.
- Command status journal details live in `docs/contracts/dispatch-status.md`.
- Keep any future orchestration/UI work outside this repo unless it is part of the bridge/runtime boundary.

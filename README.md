# Sims4 Support

Support home for the Sims 4 boundary.

First read: [docs/README.md](./docs/README.md).

Contracts and game-sidecar runtime for the Polyhydra Games Sims 4 support home.

Goal shape: canonical support home for Sims contracts and bridge runtime, while the base mod remains separate from the support boundary. `V0` is the deployable/analyzable floor and `V1` is the canonical support-home and repo-boundary pass.

This repository is the shared Sims support home. It is **not** the base mod itself; instead it defines the wire contracts and the small SignalR game-sidecar that runs resident in the game context and reports state back to the hub.

For the shared V-layer wording and checklist shape, use the canonical template in `../Api.GameServerInterop/docs/roadmap/v-layer-goals-template.md`.

## Per-Repo Fill-In

- repo name: `Sims4-Support`
- runtime sibling: none; the base mod stays separate from this support home
- support-home boundary: support home lives here; keep the base mod and gameplay ownership out of the support boundary
- local build command: `dotnet build Api.Sims4.sln`
- local test/smoke command: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- caveats: shared contracts stay in `Api.GameServerInterop`; keep bridge/runtime work explicit and do not fold the mod package into the support home

## Goal Path

This repo follows the shared infra ladder defined in `Api.GameServerInterop`.

- `V0` is the deployable/analyzable baseline: the resident game-sidecar can boot locally, a readback path can inspect state, the deployment lane is explicit, and the smoke tests prove the support shape starts locally.
- The existing Sims contract and bridge work stays in this repo; the shared telemetry, health, capability, and adapter seam stays in `Api.GameServerInterop`.
- The next layers still follow the shared ladder: `V1` canonical support home, `V2` read-only support proof, `V3` capability/control truth, `V4` public/operator projection, and `V5` approval-gated gameplay proof.
- `V3` is documented by the authoritative capability surface in `SimsModCapabilities` and `SimsCapabilitySnapshot`, plus the influence safety surface in `docs/contracts/influence-events.md`.


## V2 Read-Only Support Proof

- 252 deployment status: observed on 192.168.0.252 as of 2026-06-14; see [252 Deployment Status](../Api.GameServerInterop/docs/roadmap/252-deployment-status.md)
- deployment prerequisite: complete; the 252 lane is now live and observable
- goal: prove safe readback, inspection, or telemetry behavior without gameplay mutation
- ownership: health, version, snapshot, log-tail, and read-only projections stay in the support lane
- validation status: the repo-local test suite passes and live validation against the deployed support environment is now in place
- exit criteria: the repo exposes a stable read-only model, unsafe actions are absent or explicitly gated, and tests plus live validation cover the read-only contract shape
- avoid: mutating game state in the read-only layer or depending on unproven write access

## Validation

- repo-local tests pass: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- live validation: `scripts/deploy_252_alienware.sh` verified `/healthz`, `/state`, `/version`, container status, and host logs on `192.168.0.252`
- docs should distinguish implemented behavior from behavior that has been proven live

## Deployment Targets

- `scripts/deploy_sims4_target.sh` deploys the bridge runtime to a named target
  and verifies the read-only health surface.
- `scripts/deploy_stream_box.sh` is the first-step target for `192.168.0.178`.
- `scripts/deploy_252_alienware.sh` preserves the current `192.168.0.252` lane.
- `deploy/compose/docker-compose.yml` defines the support-home runtime adjacency
  for the selected target.
- `deploy/compose/Dockerfile` publishes the bridge into a container image.

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
  - `SimsModCapabilities` and `SimsCapabilitySnapshot` for the authoritative V3 capability surface
  - canonical action/event name catalogs
- **`Sims4.SignalR`** — the resident game-sidecar
  - connects the mod/runtime boundary to the SignalR hub
  - publishes events with retry + local buffer fallback
  - responds to heartbeat requests
- **`Tests`** — contract tests for the shared models

## Canonical action surface

The V3 capability snapshot publishes the ordered `SimsActionNames.All` catalog as the support surface.
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
The shared-contract action names `add_funds`, `add_buff`, and `send_notification` are part of the advertised catalog, but this slice does not add new gameplay semantics for them.

## Canonical event surface

The V3 capability snapshot publishes the ordered `SimsEventNames.All` catalog as the event surface.

- `Heartbeat`
- `Capabilities`
- `InventorySnapshot`
- `CommandQueued`
- `CommandCompleted`
- `CommandFailed`

## Runtime shape

1. The resident game-sidecar loads configuration.
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

See `docs/roadmaps/README.md` for the current phased backlog and planning index.
The v1 backlog lives in `docs/roadmap/v1/README.md`.
The stream-influence brainstorm and card pack live in `docs/roadmap/v1/stream-influence-events.md`.
The canonical influence-event contract lives in `docs/contracts/influence-events.md`.
The AI-facing callback-home / typed-surface goal is tracked in GitHub issue [#11](https://github.com/lancer1977/Sims4API/issues/11).

## Work tracking

- [Live Kanban tracker](docs/roadmaps/planning/Sims4-Support-KANBAN.md) — mirrors the current Sims integration cards and their parked status.

## V1 baseline

- The contracts and SignalR bridge are the durable support boundary.
- V1 covers the base mod reporting shape, command surface, and local buffering behavior.
- Future orchestration stays outside this repo unless it is part of the bridge/runtime boundary.

## Current shape

- The contracts project is the durable boundary; the base mod can evolve against it without changing the wire shape.
- The SignalR bridge carries the runtime event flow and local buffering behavior.
- Contract, config, and dispatch notes live in `docs/contracts/` and `docs/operations/`.

## Docs map

- [Docs README](./docs/README.md)
- [Deployment README](./deploy/README.md)
- [252 operator matrix](../gitops/docs/roadmaps/game-server-252-operator-matrix.md)

## Notes

- Configuration is loaded from `appsettings.json` and environment variables prefixed with `SIMS4_`.
- The repo currently defines the contracts and bridge runtime; the actual game-side mod handlers can evolve against these contracts without changing the wire shape.
- JSON wire-format details live in `docs/contracts/json-wire-format.md`.
- Influence request and decision semantics live in `docs/contracts/influence-events.md`; that doc defines the safety surface, approval gating, and cooldown behavior.
- Command status journal details live in `docs/contracts/dispatch-status.md`.
- AI consumers should stay on the typed plugin callback-home surface; do not route them through the support-sidecar boundary.
- Keep any future orchestration/UI work outside this repo unless it is part of the bridge/runtime boundary.

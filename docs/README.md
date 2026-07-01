# Sims4 Support Docs

Canonical docs entrypoint for the Sims 4 support home and resident game-sidecar.

This folder is the navigation layer for the Sims contracts, resident game-sidecar runtime, and roadmap/decision breadcrumbs. The repo is a support home, not the base mod.

The shared infra baseline for this repo is `V0` from `Api.GameServerInterop`: a bootable resident game-sidecar, a readback-capable helper path, an explicit deployment lane, and smoke checks that prove the support boundary starts cleanly locally. `V1` is the canonical support-home boundary pass.

For the shared V-layer wording and checklist shape, use the canonical template in `../../Api.GameServerInterop/docs/roadmap/v-layer-goals-template.md`.
The repo-specific bridge policy for V2 readback, V3 capability truth, V4
projection, and V5 blocking lives in
[Sims 4 V2-V4 Bridge Policy](./features/v2-v4-bridge-policy.md).

Start here for routing, then use the repository README for the concise boundary summary.

## Per-Repo Fill-In

- repo name: `Sims4-Support`
- runtime sibling: none; the base mod stays separate from this support home
- support-home boundary: support home lives here; keep the base mod and gameplay ownership out of the support boundary
- local build command: `dotnet build Api.Sims4.sln`
- local test/smoke command: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- caveats: shared contracts stay in `Api.GameServerInterop`; keep bridge/runtime work explicit and do not fold the mod package into the support home


## V2 Read-Only Support Proof

- 252 deployment status: observed on 192.168.0.252 as of 2026-06-14; see [252 Deployment Status](../../Api.GameServerInterop/docs/roadmap/252-deployment-status.md)
- deployment prerequisite: complete; the 252 lane is now live and observable
- goal: prove safe readback, inspection, or telemetry behavior without gameplay mutation
- ownership: health, version, snapshot, log-tail, and read-only projections stay in the support lane
- validation status: the repo-local test suite passes and live validation against the deployed support environment is now in place
- exit criteria: the repo exposes a stable read-only model, unsafe actions are absent or explicitly gated, and tests plus live validation cover the read-only contract shape
- avoid: mutating game state in the read-only layer or depending on unproven write access

## V3/V4 Bridge Policy

- V3 capability truth is the typed surface in `SimsModCapabilities`,
  `SimsCapabilitySnapshot`, `SimsActionNames.All`, `SimsEventNames.All`, and
  the influence gate contracts.
- V4 public/operator projection may expose redacted bridge health, version,
  capability metadata, inventory summaries, command dispatch status, and
  influence decisions.
- Projection payloads must omit secrets, web keys, approval tokens, raw command
  payloads, private identifiers, host-only file paths, and mutation handles.
- V5 gameplay/world mutation remains blocked unless a separate architecture
  names the base-mod/runtime boundary, approval policy, dry-run behavior,
  audit/result model, rollback or stop conditions, and validation smoke.

See [Sims 4 V2-V4 Bridge Policy](./features/v2-v4-bridge-policy.md).

## Validation

- repo-local tests pass: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- live validation: `scripts/deploy_252_alienware.sh` verified `/healthz`, `/state`, `/version`, container status, and host logs on `192.168.0.252`
- keep roadmap language aligned with the difference between local proof and live proof

## Deployment Targets

- [Deployment README](../deploy/README.md)
- `scripts/deploy_sims4_target.sh` is the host-deploy entry point and accepts
  `stream-box` first, with `alienware-252` as the existing live lane.
- `scripts/deploy_stream_box.sh` is the first-step entry point for
  `192.168.0.178`.
- `scripts/deploy_252_alienware.sh` preserves the `192.168.0.252` lane.
- `deploy/compose/docker-compose.yml` and `deploy/compose/Dockerfile` define the
  containerized deployment surface used by the selected lane.

## Contents

- [Repository README](../README.md)
- [Sims 4 V2-V4 Bridge Policy](features/v2-v4-bridge-policy.md)
- [Version coverage map](features/version-coverage-map.md)
- [Deployment README](../deploy/README.md)
- [Roadmap v1](roadmap/v1/README.md)
- [252 operator matrix](../../gitops/docs/roadmaps/game-server-252-operator-matrix.md)

## Decisions

- [Template](decisions/0000-template.md)

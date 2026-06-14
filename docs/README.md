# Sims4 Support Docs

Canonical docs entrypoint for the Sims 4 support home and resident game-sidecar.

This folder is the navigation layer for the Sims contracts, resident game-sidecar runtime, and roadmap/decision breadcrumbs.

The shared infra baseline for this repo is `V0` from `Api.GameServerInterop`: a bootable resident game-sidecar, a readback-capable helper path, an explicit deployment lane, and smoke checks that prove the support boundary starts cleanly locally. `V1` is the canonical support-home boundary pass.

For the shared V-layer wording and checklist shape, use the canonical template in `../../Api.GameServerInterop/docs/roadmap/v-layer-goals-template.md`.

## Per-Repo Fill-In

- repo name: `Sims4-Support`
- runtime sibling: none; the base mod stays separate from this support home
- support-home boundary: support home lives here; keep the base mod and gameplay ownership out of the support boundary
- local build command: `dotnet build Api.Sims4.sln`
- local test/smoke command: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- caveats: shared contracts stay in `Api.GameServerInterop`; keep bridge/runtime work explicit and do not fold the mod package into the support home


## V2 Read-Only Support Proof

- 252 deployment status: not observed on 192.168.0.252 as of 2026-06-13; see [252 Deployment Status](../../Api.GameServerInterop/docs/roadmap/252-deployment-status.md)
- goal: prove safe readback, inspection, or telemetry behavior without gameplay mutation
- ownership: health, version, snapshot, log-tail, and read-only projections stay in the support lane
- validation status: the repo-local test suite passes, but live validation against the deployed support environment has not been done yet
- exit criteria: the repo exposes a stable read-only model, unsafe actions are absent or explicitly gated, and tests plus live validation cover the read-only contract shape
- avoid: mutating game state in the read-only layer or depending on unproven write access

## Validation

- repo-local tests pass: `dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal`
- live validation: pending against the deployed support environment
- keep roadmap language aligned with the difference between local proof and live proof

## Contents

- [Repository README](../README.md)
- [Roadmap v1](roadmap/v1/README.md)
- [252 operator matrix](../../gitops/docs/roadmaps/game-server-252-operator-matrix.md)

## Decisions

- [Template](decisions/0000-template.md)

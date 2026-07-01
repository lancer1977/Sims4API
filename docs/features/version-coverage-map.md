---
repo: Sims4-Support
repo_shape: resident-game-sidecar-support-home
established_versions: [V0, V1, V2, V3]
planned_versions: [V4]
modeled_versions: [V5]
blocked_versions: [V5]
---

# Version Coverage Map

`Sims4-Support` is the canonical Sims support home for contracts and the
resident SignalR game-sidecar. The base mod remains outside this repository.

## Established Coverage

| Version | Status | Evidence |
| --- | --- | --- |
| V0 | Established | The bridge runtime boots, exposes `/healthz`, `/state`, `/version`, has compose/deploy assets, and has repo-local smoke tests. |
| V1 | Established | README, docs index, contracts docs, operations docs, and roadmap docs define the support-home boundary separately from the base mod. |
| V2 | Established | Health/state/version endpoints, bridge host state, event buffering, command status journals, and deployment validation prove read-only support and diagnostics. |
| V3 | Established | `SimsModCapabilities`, `SimsCapabilitySnapshot`, `SimsActionNames`, `SimsEventNames`, command dispatch status, and influence gates define the capability and control-truth surface. |

## Planned And Modeled Coverage

| Version | Status | Evidence |
| --- | --- | --- |
| V4 | Planned | Operator projection exists through bridge diagnostics and docs, but public/operator dashboards and observability outputs are not established in this repo. |
| V5 | Modeled, blocked for live proof | Influence request contracts, approval gates, cooldowns, audit trail, and command processing are tested, but live approval-gated gameplay proof belongs to the base mod/runtime integration path. |

## Validation

Run the repo-native checks:

```bash
dotnet build Api.Sims4.sln
dotnet test Tests/PolyhydraGames.Sims4.Tests/PolyhydraGames.Sims4.Tests.csproj --no-restore --nologo -v minimal
```

# Sims4 Support Kanban Tracker

Board: `default`

This tracker mirrors the live Hermes Kanban cards for the Sims integration workstream. The completed cards are recorded here for traceability. The stream influence catalog/safety gate slice and the follow-up influence slices are implemented in code and documented separately.

Those slices are backed by repo-local tests, but they have not been live-validated against the deployed support environment yet.

The current work here should stay aligned to the shared `Api.GameServerInterop` infra ladder: `V0` for the bootable resident game-sidecar and readback path, `V1` for the support-home boundary pass, then the shared `V2`-`V5` maturity steps.

| Done | Card ID | Title | Description | URL | Date added | Date modified | Status | Source |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [x] | `t_d78ff694` | Sims4 Support: lock down contract serialization and JSON schema docs | Add round-trip serialization tests for the shared contracts and document the final JSON schema for command files and event payloads. | n/a | 2026-05-31 | 2026-05-31 | done | `docs/roadmap/v1/README.md` Phase 1; `README.md` canonical action/event surface |
| [x] | `t_79e7e5f5` | Sims4 Support: add bridge buffering and command serialization regression tests | Add focused tests for buffer file format, retry backoff behavior, and command serialization boundaries. | n/a | 2026-05-31 | 2026-05-31 | done | `docs/roadmap/v1/README.md` Phase 3; SignalR bridge runtime |
| [x] | `t_9ce8fcd2` | Sims4 Support: implement mod-side command dispatcher and status persistence | Read the moddata queue, record processed/failed commands, and publish the capability handshake on startup. | n/a | 2026-05-31 | 2026-05-31 | done | `docs/roadmap/v1/README.md` Phase 2 |
| [x] | `t_18a6e912` | Sims4 Support: wire inventory handlers for add_item, take_item, and take_stock | Implement inventory add/remove handlers and inventory snapshot generation for the base-mod exposure path. | n/a | 2026-05-31 | 2026-05-31 | done | `README.md` canonical action surface; `docs/roadmap/v1/README.md` Phase 2 |
| [x] | `t_7bf33e74` | Sims4 Support: build offline smoke harness and local config docs | Add a tiny offline boot/schema harness and document the local secret/config setup. | n/a | 2026-05-31 | 2026-05-31 | done | `docs/roadmap/v1/README.md` Phase 3; `README.md` config section |
| [x] | `t_b16b0410` | Sims4 Support: publish startup capabilities snapshot handshake | Emit a timestamped capability snapshot on startup and keep the publish path visible in logs. | n/a | 2026-06-01 | 2026-06-01 | done | `docs/roadmap/v1/README.md` Phase 2; `Sims4.SignalR/Connection.cs` startup handshake |

## Completed influence follow-up cards

These were surfaced from the current thread and are kept here for traceability.

| Status | Draft title | Description | Source |
| --- | --- | --- | --- |
| done | `Sims4 Support: define stream influence event catalog and safety gates` | Canonicalize stream-triggered event names, cooldowns, approval rules, and audit logging so viewers can summon only approved incidents. | `docs/roadmap/v1/stream-influence-events.md` |
| done | `Sims4 Support: implement fire incident influence path` | Add the fire summon path and verify the status journal records the outcome cleanly. | `docs/roadmap/v1/stream-influence-events.md` |
| done | `Sims4 Support: implement robber influence path` | Add the robber / break-in summon path as a distinct event family with its own outcome handling. | `docs/roadmap/v1/stream-influence-events.md` |
| done | `Sims4 Support: implement guest summon influence path` | Add the guest / surprise visitor summon path with variants for friend, rival, neighbor, or random guest. | `docs/roadmap/v1/stream-influence-events.md` |
| done | `Sims4 Support: add stream influence smoke coverage and docs closeout` | Prove the new influence shapes are wired well enough for repo-level validation and roadmap closeout. | `docs/roadmap/v1/stream-influence-events.md` |

## Completed phase 2 cards

These cards covered the dispatcher/status slice in the current Sims workstream and are now closed out in code and tests.

| Status | Card ID | Title | Description | Source |
| --- | --- | --- | --- | --- |
| [x] | `t_9ce8fcd2` | Sims4 Support: implement mod-side command dispatcher and status persistence | Root Phase 2 card for queue consumption, failure recording, and startup handshake coordination. | `docs/roadmap/v1/README.md` Phase 2 |
| [x] | `t_f7acf1c6` | Add persistence schema for command processing status | Extend the command queue store with processing/failed metadata and troubleshooting payloads. | `docs/roadmap/v1/README.md` Phase 2 |
| [x] | `t_bdacef85` | Implement exactly-once moddata command dispatcher | Claim queue entries atomically and dispatch handlers once per command. | `docs/roadmap/v1/README.md` Phase 2 |
| [x] | `t_36f03fe1` | Add tests for idempotent dispatch and startup snapshot | Cover exactly-once queue processing, failure recording, and startup capability emission. | `docs/roadmap/v1/README.md` Phase 2 |

## Notes
- The tracker mirrors the current finished Sims integration slices in this repo.
- Do not treat local test success as live validation.
- Any future game-side mod/runtime work belongs beyond this shared-contract and queue-reader layer.
- Update this tracker in the same pass whenever any completion status changes.

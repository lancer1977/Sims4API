# Sims 4 V2-V4 Bridge Policy

Issue [#13](https://github.com/lancer1977/Sims4API/issues/13) normalizes the
Sims 4 support home onto the shared V-layer ladder while preserving the repo's
real boundary: this repo owns contracts and the resident bridge runtime, not the
base mod or unrestricted world mutation.

## Boundary

- `Sims4-Support` is the canonical support home for Sims contracts and the
  resident SignalR bridge runtime.
- The base mod stays separate from this repo and remains the gameplay/runtime
  implementation boundary.
- `Api.GameServerInterop` owns shared ladder wording and reusable cross-game
  vocabulary.
- Stream policy, approvals, routing, and operator UX stay outside this repo.

## V2 Read-Only Evidence

The V2 surface proves that the bridge can be observed without relying on
unbounded gameplay mutation.

Evidence surfaces:

- repo-local tests for contracts, command dispatch, capability snapshots,
  influence safety, queue processing, and bridge state reporting;
- bridge `/healthz`, `/state`, and `/version` endpoints;
- deployment validation through `scripts/deploy_252_alienware.sh` when live
  host proof is in scope;
- docs/contracts pages for JSON wire format, dispatch status, and influence
  event safety.

V2 remains read-only from the support perspective: health, version, bridge
state, command status, capability snapshots, inventory snapshots, and influence
gate decisions can be observed or audited, but they do not grant general world
authority.

## V3 Capability Truth

The authoritative V3 capability surface is the typed contract set:

- `SimsModCapabilities`
- `SimsCapabilitySnapshot`
- `SimsActionNames.All`
- `SimsEventNames.All`
- `SimsInfluenceGate`
- `SimsCommandDispatcher`

Capability classes:

| Capability | Current truth |
| --- | --- |
| Health, version, bridge state | Read-only and supported through the bridge endpoints. |
| Capability and inventory snapshots | Read-only/public-safe when projected with redaction rules. |
| Command dispatch status | Supported as an audit/readback surface, not as broad authority. |
| `trigger_influence` | Approval-aware and cooldown-gated through `SimsInfluenceGate`. |
| `fire_incident` and `robber_break_in` | Approval-required influence kinds. |
| `surprise_guest` | Lower-risk influence kind, still routed through typed contracts. |
| Direct base-mod world mutation | Outside this support-home boundary. |
| Free-form spawning or arbitrary player/world control | Blocked until a separate approval-gated architecture exists. |

## V4 Public And Operator Projection

The V4 projection may expose support-safe state to stream, operator, or
dashboard consumers.

Allowed projection fields:

- bridge health, version, started/connected timestamps, and last error summary;
- capability snapshot metadata and supported action/event names;
- inventory snapshot summaries after removing private or raw debug fields;
- command dispatch status, terminal state, failure code, and correlation id;
- influence decision result, kind, cooldown-until timestamp, and public-safe
  message.

Required redaction:

- no secrets, `SIMS4_` values, hub URLs with credentials, web keys, or host-only
  file paths;
- no raw command queue contents unless the payload has been explicitly
  sanitized for the target audience;
- no raw household, Sim, inventory, or target identifiers in public surfaces
  unless the downstream product has a named consent/redaction policy;
- no approval tokens in public payloads;
- no admin-only transport details or mutation handles.

Freshness and degraded-state rules:

- stale bridge state must be shown as stale or degraded, not as a successful
  live read;
- missing base-mod handlers must be reported as unavailable or blocked;
- failed command dispatch should surface a public-safe failure code and message
  without dumping raw payloads.

## V5 Block

V5 gameplay/world mutation is blocked for this repo until a separate issue and
architecture page define:

- the gameplay-capable boundary that owns mutation;
- the first harmless dev-only action;
- dry-run behavior;
- approval policy and approver identity;
- audit/result payload shape;
- rollback or stop conditions;
- validation smoke for the base-mod/runtime boundary.

Until then, `Sims4-Support` should be treated as a V2-V4 bridge and projection
surface, not as production permission to mutate Sims world state.

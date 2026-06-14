# Api.Sims4 Influence Events

This document defines the `trigger_influence` safety surface for audience-driven incidents.
It names the canonical influence kinds, the approval-gated kinds, and the cooldown behavior that govern the stream-facing control surface.

## Canonical influence kinds

These are the only influence kinds currently whitelisted by the safety gate:

- `fire_incident`
- `robber_break_in`
- `surprise_guest`

## Canonical command

Influence events are queued as `SimsCommand` items with `Action = trigger_influence` and a `SimsInfluenceRequest` payload.

### `SimsInfluenceRequest`

Base payload for `trigger_influence`.

Fields:
- `Kind` — required string, one of the canonical influence kinds
- `RequestedAt` — required timestamp
- `Reason` — optional string explaining why the influence was requested
- `Target` — optional `SimsTarget`
- `RequiresApproval` — optional boolean; if true, the request must carry an approval token
- `ApprovalToken` — optional string used by approval-gated events

### `SimsRobberBreakInRequest`

Specialized payload for the robber / break-in path.

Fields:
- `RequestedAt` — required timestamp
- `Reason` — optional string, usually the audience redemption note
- `Target` — optional `SimsTarget`
- `RequiresApproval` — defaults to `true`
- `ApprovalToken` — optional approval token
- `EntryPoint` — optional entry-point label
- `Severity` — optional string, defaults to `medium`
- `Stealthy` — optional boolean, defaults to `true`
- `PoliceNotified` — optional boolean, defaults to `true`

Use this shape when the command is `trigger_influence` and `Kind = robber_break_in`.

### `SimsSurpriseGuestRequest`

Specialized payload for the surprise guest path.

Fields:
- `RequestedAt` — required timestamp
- `Reason` — optional string, usually the audience redemption note
- `Target` — optional `SimsTarget`
- `GuestVariant` — one of `friend`, `rival`, `neighbor`, or `random_guest`
- `GuestName` — optional guest label
- `Venue` — optional venue / room label
- `AnnounceArrival` — defaults to `true`
- `RequiresApproval` — defaults to `false`
- `ApprovalToken` — optional approval token

Use this shape when the command is `trigger_influence` and `Kind = surprise_guest`.

### `SimsFireIncidentRequest`

Specialized payload for the fire incident path.

Fields:
- `RequestedAt` — required timestamp
- `Reason` — optional string, usually the audience redemption note
- `Target` — optional `SimsTarget`
- `RequiresApproval` — defaults to `true`
- `ApprovalToken` — optional approval token
- `Room` — optional room/location label
- `Severity` — optional string, defaults to `medium`
- `NotifyFirefighters` — optional boolean, defaults to `true`

Use this shape when the command is `trigger_influence` and `Kind = fire_incident`.


```json
{
  "StreamerUserId": "streamer-1",
  "Action": "trigger_influence",
  "Timestamp": "2026-05-31T12:00:00Z",
  "Payload": {
    "Kind": "surprise_guest",
    "RequestedAt": "2026-05-31T12:00:00Z",
    "Reason": "chat redeem",
    "RequiresApproval": false
  },
  "Id": "cmd-influence-1"
}
```

## Safety gates

The in-memory `SimsInfluenceGate` applies the following checks before the handler is allowed to process the request:

1. The kind must be present.
2. The kind must be whitelisted.
3. Approval-gated kinds must include an approval token.
4. The same kind is subject to a cooldown window so viewers cannot spam the same incident.

By default the approval-gated kinds are:

- `fire_incident`
- `robber_break_in`

The default cooldown is 10 minutes per kind.

## Audit trail

The gate records a decision entry for every request so the mod can explain why an influence was accepted or rejected.

Decision fields:
- `StreamerUserId`
- `Kind`
- `RequestedAt`
- `EvaluatedAt`
- `Allowed`
- `Message`
- `RejectionReason`
- `CooldownUntil`
- `Target`
- `Reason`

Example rejected decision:
```json
{
  "StreamerUserId": "streamer-1",
  "Kind": "fire_incident",
  "RequestedAt": "2026-05-31T12:03:00Z",
  "EvaluatedAt": "2026-05-31T12:04:00Z",
  "Allowed": false,
  "Message": "Influence kind 'fire_incident' is cooling down until 2026-05-31T12:10:00.0000000+00:00.",
  "RejectionReason": "Influence kind 'fire_incident' is cooling down until 2026-05-31T12:10:00.0000000+00:00.",
  "CooldownUntil": "2026-05-31T12:10:00Z",
  "Target": {
    "HouseholdId": "household-9"
  },
  "Reason": "chat redeem"
}
```

## Non-goals

- No unbounded free-form event spawning.
- No transport rewrite in this slice.
- No attempt to model every Sims incident family at once.

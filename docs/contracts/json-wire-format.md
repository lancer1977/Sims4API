# Sims 4 JSON wire format

This document captures the current contract shapes used by `Api.Sims4`.
The serializer currently emits the CLR property names as written in the shared records.
The authoritative V3 capability surface is `SimsModCapabilities` plus `SimsCapabilitySnapshot`; influence request and decision semantics live in `docs/contracts/influence-events.md`, which defines the safety surface, approval gating, and cooldown behavior.

## Common notes

- `Payload` is intentionally opaque and may contain arbitrary JSON.
- `Target` is optional on command envelopes and snapshots.
- All timestamps are `DateTimeOffset` values.
- Collections are emitted as JSON arrays.

## `StreamEvent`

Outbound event envelope used by the bridge when publishing state back to the hub.

```json
{
  "StreamerUserId": "streamer-1",
  "Type": "CommandQueued",
  "Timestamp": "2026-05-31T12:00:00+00:00",
  "Payload": {
    "action": "take_stock",
    "count": 3
  },
  "Id": "evt-1"
}
```

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `StreamerUserId` | string | yes | Streamer/web-key identity for the event source. |
| `Type` | string | yes | Canonical event name such as `Heartbeat` or `InventorySnapshot`. |
| `Timestamp` | string (`DateTimeOffset`) | yes | ISO 8601 timestamp. |
| `Payload` | object | yes | Opaque event payload. |
| `Id` | string | yes | Idempotency/event identifier. |

## `SimsCommand`

Inbound command envelope used by the mod-side dispatcher.

```json
{
  "StreamerUserId": "streamer-1",
  "Action": "add_item",
  "Timestamp": "2026-05-31T12:05:00+00:00",
  "Payload": {
    "ItemId": "item-1",
    "DisplayName": "Laser Cutter",
    "Quantity": 2,
    "IsStackable": false,
    "Category": "Debug",
    "Source": "Spawner"
  },
  "Id": "cmd-1",
  "Target": {
    "SimId": "sim-42",
    "InventoryScope": "household"
  },
  "CorrelationId": "corr-77"
}
```

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `StreamerUserId` | string | yes | Streamer/web-key identity for the command. |
| `Action` | string | yes | Canonical action name. |
| `Timestamp` | string (`DateTimeOffset`) | yes | ISO 8601 timestamp. |
| `Payload` | object | yes | Opaque command payload. |
| `Id` | string | yes | Idempotency/command identifier. |
| `Target` | `SimsTarget` | no | Optional routing target. |
| `CorrelationId` | string | no | Optional cross-system correlation id. |

## `SimsAddItemRequest`

Payload for `add_item`.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `ItemId` | string | yes | Stable item identifier. |
| `DisplayName` | string | yes | Human-readable item name. |
| `Quantity` | integer | no | Defaults to `1`. |
| `IsStackable` | boolean | no | Defaults to `true`. |
| `Category` | string | no | Optional classification label. |
| `Source` | string | no | Optional source/origin label. |

## `SimsTakeItemRequest`

Payload for `take_item`.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `ItemId` | string | yes | Stable item identifier. |
| `Quantity` | integer | no | Defaults to `1`. |
| `Notes` | string | no | Optional note about the removal. |

## `SimsTakeStockRequest`

Payload for `take_stock`.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `Notes` | string | no | Optional note for the snapshot. |

## `SimsRunInteractionRequest`

Payload for `run_interaction`.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `RequestedAt` | string (`DateTimeOffset`) | yes | Timestamp for when the request was created. |
|| `InteractionId` | string | yes | Stable interaction identifier. |
|| `InteractionName` | string | no | Optional human-readable interaction name. |
|| `Target` | `SimsTarget` | no | Optional target context for the interaction. |
|| `QueueContext` | string | no | Optional note describing the queue source or context. |
|| `RequiresApproval` | boolean | no | Optional boolean; defaults to `false`. |
|| `ApprovalToken` | string | no | Optional approval token supplied by an operator. |

## `SimsSpawnObjectRequest`

Payload for `spawn_object`.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `RequestedAt` | string (`DateTimeOffset`) | yes | Timestamp for when the request was created. |
|| `ObjectId` | string | yes | Stable object identifier. |
|| `DisplayName` | string | yes | Human-readable object name. |
|| `Target` | `SimsTarget` | no | Optional target context for the spawn. |
|| `Quantity` | integer | no | Defaults to `1`. |
|| `Placement` | string | no | Optional placement label such as `kitchen counter` or `front yard`. |
|| `Notes` | string | no | Optional human-readable note for the spawn request. |
|| `RequiresApproval` | boolean | no | Optional boolean; defaults to `false`. |
|| `ApprovalToken` | string | no | Optional approval token supplied by an operator. |

## `SimsRobberBreakInRequest`

Specialized payload for the `robber_break_in` path.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `RequestedAt` | string (`DateTimeOffset`) | yes | Timestamp for when the request was created. |
|| `Reason` | string | no | Optional human-readable reason or redemption note. |
|| `Target` | `SimsTarget` | no | Optional target context for the influence. |
|| `RequiresApproval` | boolean | no | Defaults to `true` for this path. |
|| `ApprovalToken` | string | no | Optional approval token supplied by an operator. |
|| `EntryPoint` | string | no | Optional entry-point label. |
|| `Severity` | string | no | Optional severity string such as `low`, `medium`, or `high`. |
|| `Stealthy` | boolean | no | Optional boolean; defaults to `true`. |
|| `PoliceNotified` | boolean | no | Optional boolean; defaults to `true`. |

## `SimsSurpriseGuestRequest`

Specialized payload for the `surprise_guest` path.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `RequestedAt` | string (`DateTimeOffset`) | yes | Timestamp for when the request was created. |
|| `Reason` | string | no | Optional human-readable reason or redemption note. |
|| `Target` | `SimsTarget` | no | Optional target context for the influence. |
|| `GuestVariant` | string | no | One of `friend`, `rival`, `neighbor`, or `random_guest`. |
|| `GuestName` | string | no | Optional guest label. |
|| `Venue` | string | no | Optional venue / room label. |
|| `AnnounceArrival` | boolean | no | Optional boolean; defaults to `true`. |
|| `RequiresApproval` | boolean | no | Optional boolean; defaults to `false`. |
|| `ApprovalToken` | string | no | Optional approval token supplied by an operator. |

## `SimsFireIncidentRequest`

Specialized payload for the `fire_incident` path.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `RequestedAt` | string (`DateTimeOffset`) | yes | Timestamp for when the request was created. |
|| `Reason` | string | no | Optional human-readable reason or redemption note. |
|| `Target` | `SimsTarget` | no | Optional target context for the influence. |
|| `RequiresApproval` | boolean | no | Defaults to `true` for this path. |
|| `ApprovalToken` | string | no | Optional approval token supplied by an operator. |
|| `Room` | string | no | Optional room/location label. |
|| `Severity` | string | no | Optional severity string such as `low`, `medium`, or `high`. |
|| `NotifyFirefighters` | boolean | no | Optional boolean; defaults to `true`. |

## `SimsTarget`

Targeting hints for a command or snapshot.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `SimId` | string | no | Target sim identifier. |
| `HouseholdId` | string | no | Target household identifier. |
| `ObjectId` | string | no | Target object identifier. |
| `InventoryScope` | string | no | Scope such as `household`, `personal`, or `world`. |

## `SimsInventoryItem`

A single inventory entry.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `ItemId` | string | yes | Stable item identifier. |
| `DisplayName` | string | yes | Human-readable item name. |
| `Quantity` | integer | yes | Item count. |
| `IsStackable` | boolean | yes | Whether the item can be stacked. |
| `Category` | string | no | Optional classification label. |
| `Source` | string | no | Optional source/origin label. |

## `SimsInventorySnapshot`

Inventory state captured for stocktake or exposure reporting.

| Property | Type | Required | Notes |
| --- | --- | --- | --- |
| `StreamerUserId` | string | yes | Streamer/web-key identity for the snapshot. |
| `CapturedAt` | string (`DateTimeOffset`) | yes | ISO 8601 timestamp. |
| `Target` | `SimsTarget` | no | Optional target context. |
| `Items` | array of `SimsInventoryItem` | yes | Inventory entries at the capture point. |
| `HouseholdFunds` | integer | no | Optional household cash amount. |
| `Notes` | string | no | Optional human-readable note. |

## `SimsModCapabilities`

Authoritative V3 capability handshake published by the mod on startup.
`SupportedActions` is the ordered `SimsActionNames.All` catalog, and `SupportedEvents` is the ordered `SimsEventNames.All` catalog.
These arrays are the source of truth for the advertised capability surface.

|| Property | Type | Required | Notes |
|| --- | --- | --- | --- |
|| `ModVersion` | string | yes | Mod build version. |
|| `ApiVersion` | string | yes | Contract/API version. |
||| `SupportedActions` | array of string | yes | Canonical action catalog exposed by the mod. |
||| `SupportedEvents` | array of string | yes | Event names the mod can publish. |
||| `SupportsInventoryExposure` | boolean | yes | Indicates inventory snapshot support. |
||| `SupportsStocktake` | boolean | yes | Indicates stocktake support. |

## `SimsCapabilitySnapshot`

Timestamped startup payload that wraps the authoritative V3 capability surface before it is emitted to the hub.

||| Property | Type | Required | Notes |
||| --- | --- | --- | --- |
||| `CapturedAt` | string (`DateTimeOffset`) | yes | Snapshot timestamp in UTC. |
||| `Capabilities` | `SimsModCapabilities` | yes | Deterministic capability surface published on boot. |

## `SimsInfluenceDecision`

`SimsInfluenceDecision` is the in-memory audit shape produced by the influence gate when it accepts or rejects a request.

Fields:
- `StreamerUserId` — required string
- `Kind` — required string
- `RequestedAt` — required timestamp
- `EvaluatedAt` — required timestamp
- `Allowed` — required boolean
- `Message` — required string
- `RejectionReason` — optional string
- `CooldownUntil` — optional timestamp
- `Target` — optional `SimsTarget`
- `Reason` — optional string

Example decision entry:
```json
{
  "StreamerUserId": "streamer-1",
  "Kind": "surprise_guest",
  "RequestedAt": "2026-05-31T12:00:00Z",
  "EvaluatedAt": "2026-05-31T12:00:00Z",
  "Allowed": true,
  "Message": "Approved influence 'surprise_guest' for streamer-1: chat redeem",
  "RejectionReason": null,
  "CooldownUntil": null,
  "Target": null,
  "Reason": "chat redeem"
}
```

## `SimsCommandDispatchRecord`

## `SimsCommandDispatchRecord`

||| Property | Type | Required | Notes |
||| --- | --- | --- | --- |
||| `CommandId` | string | yes | Idempotency key for the inbound command. |
||| `StreamerUserId` | string | yes | Streamer/web-key identity for the source. |
||| `Action` | string | yes | Canonical command action. |
||| `CommandTimestamp` | string (`DateTimeOffset`) | yes | Timestamp supplied by the caller. |
||| `DispatchedAt` | string (`DateTimeOffset`) | yes | Local processing timestamp and base command transition anchor. |
||| `Status` | string | yes | `Unhandled`, `Pending`, `Processing`, `Processed`, or `Failed`. |
||| `HandlerName` | string | no | Handler that processed the command, when available. |
||| `Message` | string | no | Human-readable note. |
||| `Target` | `SimsTarget` | no | Optional routing hint. |
||| `CorrelationId` | string | no | Optional cross-system correlation id. |
||| `ProcessingAt` | string (`DateTimeOffset`) | no | Timestamp for the `Processing` transition. |
||| `ProcessedAt` | string (`DateTimeOffset`) | no | Timestamp for the `Processed` terminal transition. |
||| `FailedAt` | string (`DateTimeOffset`) | no | Timestamp for the `Failed`/`Unhandled` terminal transition. |
||| `FailureReason` | string | no | Optional root-cause reason. |
||| `FailureCode` | string | no | Optional failure category code. |
||| `FailureStack` | string | no | Optional diagnostic stack trace. |
||| `RawErrorPayload` | string | no | Optional serialized payload attached to failure details. |

# Command dispatch status journal

This document describes the local status journal used by the dispatcher foundation.
The record shape is intentionally simple so the mod-side queue reader can persist processed, failed, and unhandled outcomes without depending on a broader storage stack.

## `SimsCommandDispatchRecord`

Each record is written as a JSONL line.
The journal helper writes a new line for each transition, which keeps status changes append-only while still allowing atomic checks on the latest record for a command id.

```json
{
  "CommandId": "cmd-1",
  "StreamerUserId": "streamer-1",
  "Action": "add_item",
  "CommandTimestamp": "2026-05-31T12:05:00+00:00",
  "DispatchedAt": "2026-05-31T12:05:03+00:00",
  "Status": "Processed",
  "HandlerName": "FakeHandler",
  "Message": "queued",
  "Target": {
    "SimId": "sim-42",
    "InventoryScope": "household"
  },
  "CorrelationId": "corr-77",
  "ProcessingAt": "2026-05-31T12:05:03+00:00",
  "ProcessedAt": "2026-05-31T12:05:03+00:00",
  "FailedAt": null,
  "FailureReason": null,
  "FailureCode": null,
  "FailureStack": null,
  "RawErrorPayload": null
}
```

### Fields

- `CommandId`: idempotency key for the inbound command.
- `StreamerUserId`: bridge/web-key identity for the source.
- `Action`: canonical command action.
- `CommandTimestamp`: timestamp supplied by the caller.
- `DispatchedAt`: local processing timestamp and base command transition anchor.
- `Status`: `Unhandled`, `Pending`, `Processing`, `Processed`, or `Failed`.
- `HandlerName`: the handler that processed the command, when available.
- `Message`: a human-readable success/failure note.
- `Target`: optional routing hint.
- `CorrelationId`: optional cross-system correlation identifier.
- `ProcessingAt`: timestamp for `Processing` transition, when present.
- `ProcessedAt`: timestamp for `Processed` terminal state, when present.
- `FailedAt`: timestamp for `Failed`/`Unhandled` terminal state, when present.
- `FailureReason`: optional root-cause summary for failed terminal states.
- `FailureCode`: optional classification code for failed terminal states.
- `FailureStack`: optional failure stack trace for terminal failures.
- `RawErrorPayload`: optional serialized payload from the command or handler failure context.

## Storage shape

The default journal file name is `command-status.jsonl`. Each line is appended independently so the queue reader can write status updates without rewriting the whole file.

If a queue claim is interrupted, the mod leaves behind a `*.processing.jsonl` snapshot. On the next pass, the runtime replays the claimed snapshot, skips any command that already reached a terminal status, and then deletes the snapshot once the batch is drained.

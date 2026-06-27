# Sims4 Support Feature Index

Canonical docs entrypoint for Sims 4 support capabilities.

This repo tracks the durable contracts, bridge runtime, and local operational
guidance that define the support boundary.

## Entry Points

- [Repository README](../../README.md)
- [Docs README](../README.md)

## Durable Features

- [Sims 4 V2-V4 Bridge Policy](./v2-v4-bridge-policy.md)
- [Influence-event contract](../contracts/influence-events.md)
- [JSON wire format](../contracts/json-wire-format.md)
- [Dispatch status](../contracts/dispatch-status.md)
- [Local config](../operations/local-config.md)

## Work Surface

- [Roadmap v1](../roadmap/v1/README.md)
- [Stream influence events](../roadmap/v1/stream-influence-events.md)

## Notes

- Keep the shared wire contracts stable.
- Keep the bridge/runtime surface small and explicit.
- Keep V5 gameplay/world mutation blocked until a separate approval-gated
  architecture owns it.
- Move speculative UI or orchestration ideas into roadmap pages until they are proven.

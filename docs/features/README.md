# Sims4 Support Feature Index

Canonical docs entrypoint for Sims 4 support capabilities.

This repo tracks the durable contracts, bridge runtime, and local operational
guidance that define the support boundary.

## Entry Points

- [Repository README](../../README.md)
- [Docs README](../README.md)

## Durable Features

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
- Move speculative UI or orchestration ideas into roadmap pages until they are proven.

# Sims4 Support Roadmaps

Canonical roadmap entrypoint for the Sims 4 support home.

## Current Roadmap

- [v1](../roadmap/v1/README.md)
- [Stream influence events](../roadmap/v1/stream-influence-events.md)

## AI Coverage

- The typed callback-home surface for `UI`, `AI`, and `MCP` consumers stays on the plugin/callback-home side and is documented in the roadmap and contract notes; it was tracked in GitHub issue [#11](https://github.com/lancer1977/Sims4API/issues/11) and is now closed.
- Keep AI-facing routing on the plugin/callback-home side, not the support-sidecar boundary.

## Support-Sidecar Coverage

- The read-only health, version, snapshot, and state-projection surface belongs to the support-sidecar boundary and was tracked in GitHub issue [#12](https://github.com/lancer1977/Sims4API/issues/12) and is now closed.
- Keep the sidecar support-only and read-only; gameplay authority stays outside this boundary.

## Notes

- Keep active roadmap language tied to the current bridge/runtime boundary.
- Keep parked ideas in the roadmap pages themselves instead of a separate planning tracker.

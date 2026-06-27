# Sims4 Support Roadmaps

Canonical roadmap entrypoint for the Sims 4 support home.

## Current Roadmap

- [Sims 4 V2-V4 Bridge Policy](../features/v2-v4-bridge-policy.md)
- [v1](../roadmap/v1/README.md)
- [Stream influence events](../roadmap/v1/stream-influence-events.md)

## V2-V4 Policy

- GitHub issue [#13](https://github.com/lancer1977/Sims4API/issues/13)
  normalized this repo as a V2-V4 bridge/runtime surface.
- V2 is read-only bridge evidence: local tests, `/healthz`, `/state`,
  `/version`, and deploy smoke when live validation is in scope.
- V3 is typed capability/control truth through `SimsModCapabilities`,
  `SimsCapabilitySnapshot`, action/event catalogs, influence gates, and command
  dispatch records.
- V4 is the redacted public/operator projection layer described in the bridge
  policy.
- V5 gameplay/world mutation stays blocked until a separate approval-gated
  architecture defines the base-mod/runtime boundary and validation smoke.

## AI Coverage

- The typed callback-home surface for `UI`, `AI`, and `MCP` consumers stays on the plugin/callback-home side and is documented in the roadmap and contract notes; it was tracked in GitHub issue [#11](https://github.com/lancer1977/Sims4API/issues/11) and is now closed.
- Keep AI-facing routing on the plugin/callback-home side, not the support-sidecar boundary.

## Support-Sidecar Coverage

- The read-only health, version, snapshot, and state-projection surface belongs to the support-sidecar boundary and was tracked in GitHub issue [#12](https://github.com/lancer1977/Sims4API/issues/12) and is now closed.
- Keep the sidecar support-only and read-only; gameplay authority stays outside this boundary.

## Notes

- Keep active roadmap language tied to the current bridge/runtime boundary.
- Keep parked ideas in the roadmap pages themselves instead of a separate planning tracker.

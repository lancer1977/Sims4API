# deploy/compose

Canonical `V2` deployment contract for the Sims4 support home.

## Purpose

- `docker-compose.yml` describes the support-home runtime adjacency.
- The bridge runtime hosts a read-only `/healthz` surface and a local SignalR
  hub so the deployment lane stays observable without mutating game state.
- `scripts/deploy_sims4_target.sh` syncs the repo to the selected host,
  brings the compose stack up, and verifies the deployed bridge container plus
  the health endpoint.
- `stream-box` is the first-step target; `alienware-252` stays available as the
  existing live lane.

## Next step

Keep the deployment lane narrow and keep gameplay ownership out of this repo.

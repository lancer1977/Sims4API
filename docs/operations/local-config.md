# Local bridge config

The bridge loads `appsettings.json` from the app base path and then applies environment variables prefixed with `SIMS4_`.

## Minimal sample

```json
{
  "HubUrl": "https://localhost/signalr",
  "WebKey": "sample-web-key",
  "EventBufferPath": "buffer.jsonl",
  "RetryAttempts": 5,
  "RetryDelayMilliseconds": 500
}
```

## Environment variables

Each setting can also be supplied through the environment:

- `SIMS4_HubUrl`
- `SIMS4_WebKey`
- `SIMS4_EventBufferPath`
- `SIMS4_RetryAttempts`
- `SIMS4_RetryDelayMilliseconds`

## Smoke check

The repo includes a boot smoke test that loads a sample `appsettings.json`, binds `SimsBridgeOptions`, and resolves the bridge connection without starting the SignalR network path.

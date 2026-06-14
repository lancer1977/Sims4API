using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Bridge;

public sealed class SimsBridgeHostState
{
    private readonly object _gate = new();

    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _connectedAt;
    private string? _lastError;
    private string? _lastEventType;
    private string? _lastEventId;

    public void MarkStarting()
    {
        lock (_gate)
        {
            _startedAt = DateTimeOffset.UtcNow;
            _connectedAt = null;
            _lastError = null;
        }
    }

    public void MarkConnected()
    {
        lock (_gate)
        {
            _connectedAt = DateTimeOffset.UtcNow;
            _lastError = null;
        }
    }

    public void MarkFailed(Exception ex)
    {
        lock (_gate)
        {
            _lastError = ex.Message;
        }
    }

    public void MarkEvent(StreamEvent evt)
    {
        lock (_gate)
        {
            _lastEventType = evt.Type;
            _lastEventId = evt.Id;
        }
    }

    public SimsBridgeReadOnlySnapshot CreateHealthReport(SimsBridgeOptions options)
    {
        var state = CreateStateReport(options);
        var healthy = state.ConnectedAt is not null && string.IsNullOrWhiteSpace(state.LastError);

        return new SimsBridgeReadOnlySnapshot
        {
            Status = healthy ? "ok" : "degraded",
            Surface = "Sims4.SignalR",
            Bridge = healthy ? "connected" : "starting",
            StartedAt = state.StartedAt,
            ConnectedAt = state.ConnectedAt,
            LastEventType = state.LastEventType,
            LastEventId = state.LastEventId,
            HubUrl = state.HubUrl,
            LastError = state.LastError
        };
    }

    public SimsBridgeReadOnlySnapshot CreateStateReport(SimsBridgeOptions options)
    {
        lock (_gate)
        {
            return new SimsBridgeReadOnlySnapshot
            {
                Surface = "Sims4.SignalR",
                StartedAt = _startedAt,
                ConnectedAt = _connectedAt,
                LastEventType = _lastEventType,
                LastEventId = _lastEventId,
                HubUrl = options.HubUrl,
                LastError = _lastError
            };
        }
    }
}

public sealed record SimsBridgeReadOnlySnapshot
{
    public string? Status { get; init; }
    public string? Surface { get; init; }
    public string? Bridge { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? ConnectedAt { get; init; }
    public string? LastEventType { get; init; }
    public string? LastEventId { get; init; }
    public string? HubUrl { get; init; }
    public string? LastError { get; init; }
}

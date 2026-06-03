using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Bridge;

public interface ISimsBridgeHubConnection
{
    void OnRequestHeartbeat(Func<string, Task> handler);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task InvokeAsync(string methodName, object?[] arguments, CancellationToken cancellationToken = default);
}

public sealed class Connection : IAsyncDisposable
{
    private const string ApiVersion = "v1";

    private readonly ISimsBridgeHubConnection _hub;
    private readonly SimsBridgeOptions _options;
    private readonly ILogger<Connection> _logger;

    public Connection(IOptions<SimsBridgeOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = NullLogger<Connection>.Instance;
        _hub = new HubConnectionAdapter(CreateHubConnection(_options));
    }

    internal Connection(IOptions<SimsBridgeOptions> options, ISimsBridgeHubConnection hub, ILogger<Connection> logger)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
        _logger = logger ?? NullLogger<Connection>.Instance;
    }

    public string WebKey => _options.WebKey;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _hub.OnRequestHeartbeat(async _ => await PublishHeartbeatAsync(cancellationToken));

        _logger.LogInformation("Starting Sims bridge for {WebKey}.", WebKey);
        await _hub.StartAsync(cancellationToken);
        await PublishStartupCapabilitiesAsync(cancellationToken);
    }

    public Task PublishHeartbeatAsync(CancellationToken cancellationToken = default) =>
        PublishEventAsync(SimsEventNames.Heartbeat, new { status = "ok" }, cancellationToken: cancellationToken);

    public Task PublishCapabilitiesAsync(SimsModCapabilities capabilities, CancellationToken cancellationToken = default) =>
        PublishEventAsync(SimsEventNames.Capabilities, capabilities, cancellationToken: cancellationToken);

    public Task PublishCapabilitySnapshotAsync(SimsCapabilitySnapshot snapshot, CancellationToken cancellationToken = default) =>
        PublishEventAsync(SimsEventNames.Capabilities, snapshot, cancellationToken: cancellationToken);

    public Task PublishInventorySnapshotAsync(SimsInventorySnapshot snapshot, CancellationToken cancellationToken = default) =>
        PublishEventAsync(SimsEventNames.InventorySnapshot, snapshot, cancellationToken: cancellationToken);

    public Task PublishCommandQueuedAsync(SimsCommand command, CancellationToken cancellationToken = default) =>
        PublishEventAsync(SimsEventNames.CommandQueued, command, cancellationToken: cancellationToken);

    public Task PublishEventAsync(string type, object payload, string? id = null, CancellationToken cancellationToken = default) =>
        SubmitEventAsync(new StreamEvent(WebKey, type, DateTimeOffset.UtcNow, payload, id ?? Guid.NewGuid().ToString("N")), cancellationToken);

    private static HubConnection CreateHubConnection(SimsBridgeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HubUrl))
        {
            throw new InvalidOperationException("Sims bridge HubUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(options.WebKey))
        {
            throw new InvalidOperationException("Sims bridge WebKey is required.");
        }

        return new HubConnectionBuilder()
            .WithUrl($"{options.HubUrl}?streamerId={Uri.EscapeDataString(options.WebKey)}")
            .WithAutomaticReconnect()
            .Build();
    }

    private async Task PublishStartupCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = SimsCapabilitySnapshotFactory.Create(DateTimeOffset.UtcNow, GetModVersion(), ApiVersion);
        _logger.LogInformation(
            "Publishing Sims capability snapshot with {ActionCount} actions and {EventCount} events.",
            snapshot.Capabilities.SupportedActions.Count,
            snapshot.Capabilities.SupportedEvents.Count);

        await PublishCapabilitySnapshotAsync(snapshot, cancellationToken);
    }

    private async Task SubmitEventAsync(StreamEvent evt, CancellationToken cancellationToken = default)
    {
        var attempts = Math.Max(1, _options.RetryAttempts);
        var delivered = false;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await _hub.InvokeAsync("SubmitEvent", new object?[] { evt }, cancellationToken);
                delivered = true;
                break;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (attempt == attempts)
                {
                    _logger.LogWarning(ex, "Failed to publish Sims event {Type} after {Attempts} attempt(s); falling back to local buffer.", evt.Type, attempts);
                    break;
                }

                var delay = Math.Max(1, _options.RetryDelayMilliseconds) * attempt;
                _logger.LogWarning(ex, "Publish attempt {Attempt}/{Attempts} failed for Sims event {Type}; retrying in {Delay} ms.", attempt, attempts, evt.Type, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        if (delivered)
        {
            return;
        }

        try
        {
            await QueueForLaterAsync(evt, cancellationToken);
            _logger.LogWarning(lastError, "Buffered Sims event {Type} after hub delivery failed.", evt.Type);
        }
        catch (Exception bufferError)
        {
            _logger.LogError(bufferError, "Failed to publish or buffer Sims event {Type}.", evt.Type);
            throw;
        }
    }

    private async Task QueueForLaterAsync(StreamEvent evt, CancellationToken cancellationToken = default)
    {
        await SimsBridgeBuffer.AppendAsync(evt, _options.EventBufferPath, cancellationToken);
    }

    private string GetModVersion()
    {
        var version = typeof(Connection).Assembly.GetName().Version;
        return version?.ToString() ?? "0.0.0";
    }

    public ValueTask DisposeAsync() => _hub is HubConnectionAdapter adapter ? adapter.DisposeAsync() : ValueTask.CompletedTask;

    private sealed class HubConnectionAdapter(HubConnection connection) : ISimsBridgeHubConnection
    {
        private readonly HubConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public void OnRequestHeartbeat(Func<string, Task> handler)
        {
            _connection.On<string>("requestHeartbeat", handler);
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => _connection.StartAsync(cancellationToken);

        public Task InvokeAsync(string methodName, object?[] arguments, CancellationToken cancellationToken = default) =>
            _connection.InvokeAsync(methodName, arguments, cancellationToken);

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

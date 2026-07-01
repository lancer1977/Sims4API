using System.Text.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PolyhydraGames.Sims4.Bridge;
using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Tests;

public sealed class SimsCapabilitySnapshotTests
{
    [Test]
    public void Version_coverage_map_tags_established_bridge_and_capability_layers()
    {
        var repoRoot = FindRepoRoot();
        var coverageMap = File.ReadAllText(Path.Combine(repoRoot, "docs", "features", "version-coverage-map.md"));

        Assert.That(coverageMap, Does.Contain("established_versions: [V0, V1, V2, V3]"));
        Assert.That(coverageMap, Does.Contain("planned_versions: [V4]"));
        Assert.That(coverageMap, Does.Contain("modeled_versions: [V5]"));
        Assert.That(coverageMap, Does.Contain("blocked_versions: [V5]"));
    }

    [Test]
    public void SnapshotFactory_BuildsADeterministicPhaseTwoSurface()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 31, 12, 34, 56, TimeSpan.Zero);

        var snapshot = SimsCapabilitySnapshotFactory.Create(capturedAt, modVersion: "1.2.3", apiVersion: "v1");

        Assert.That(snapshot.CapturedAt, Is.EqualTo(capturedAt));
        Assert.That(snapshot.Capabilities.ModVersion, Is.EqualTo("1.2.3"));
        Assert.That(snapshot.Capabilities.ApiVersion, Is.EqualTo("v1"));
        Assert.That(snapshot.Capabilities.SupportedActions, Is.EqualTo(SimsActionNames.All.OrderBy(action => action, StringComparer.Ordinal).ToArray()));
        Assert.That(snapshot.Capabilities.SupportedEvents, Is.EqualTo(SimsEventNames.All.OrderBy(evt => evt, StringComparer.Ordinal).ToArray()));
        Assert.That(snapshot.Capabilities.SupportsInventoryExposure, Is.True);
        Assert.That(snapshot.Capabilities.SupportsStocktake, Is.True);

        var json = JsonSerializer.Serialize(snapshot);
        using var doc = JsonDocument.Parse(json);

        Assert.That(doc.RootElement.GetProperty("CapturedAt").GetDateTimeOffset(), Is.EqualTo(capturedAt));
        Assert.That(doc.RootElement.GetProperty("Capabilities").GetProperty("SupportedActions").EnumerateArray().Select(x => x.GetString()).ToArray(),
            Is.EqualTo(SimsActionNames.All.OrderBy(action => action, StringComparer.Ordinal).ToArray()));
    }

    [Test]
    public async Task Connection_StartAsync_PublishesExactlyOneCapabilitySnapshot()
    {
        var options = Options.Create(new SimsBridgeOptions
        {
            HubUrl = "https://localhost/signalr",
            WebKey = "sample-web-key",
        });

        var beforeStart = DateTimeOffset.UtcNow;
        var hub = new FakeHubConnection();
        await using var connection = new Connection(options, hub, NullLogger<Connection>.Instance);

        await connection.StartAsync();
        var afterStart = DateTimeOffset.UtcNow;

        Assert.That(hub.StartCalls, Is.EqualTo(1));
        Assert.That(hub.Handlers, Does.ContainKey("requestHeartbeat"));
        Assert.That(hub.Invocations, Has.Count.EqualTo(1));
        Assert.That(hub.Invocations[0].Method, Is.EqualTo("SubmitEvent"));

        var evt = hub.Invocations[0].Arguments[0];
        Assert.That(evt, Is.TypeOf<StreamEvent>());

        var streamEvent = (StreamEvent)evt;
        Assert.That(streamEvent.Type, Is.EqualTo(SimsEventNames.Capabilities));
        Assert.That(streamEvent.StreamerUserId, Is.EqualTo("sample-web-key"));

        var snapshot = streamEvent.Payload as SimsCapabilitySnapshot;
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.CapturedAt, Is.InRange(beforeStart, afterStart));
        Assert.That(snapshot.Capabilities.ModVersion, Is.EqualTo(typeof(Connection).Assembly.GetName().Version?.ToString()));
        Assert.That(snapshot.Capabilities.ApiVersion, Is.EqualTo("v1"));
        Assert.That(snapshot.Capabilities.SupportedActions, Is.EqualTo(SimsActionNames.All.OrderBy(action => action, StringComparer.Ordinal).ToArray()));
        Assert.That(snapshot.Capabilities.SupportedEvents, Is.EqualTo(SimsEventNames.All.OrderBy(evt => evt, StringComparer.Ordinal).ToArray()));
        Assert.That(snapshot.Capabilities.SupportsInventoryExposure, Is.True);
        Assert.That(snapshot.Capabilities.SupportsStocktake, Is.True);
    }

    private sealed class FakeHubConnection : ISimsBridgeHubConnection
    {
        public int StartCalls { get; private set; }

        public Dictionary<string, Delegate> Handlers { get; } = new(StringComparer.Ordinal);

        public List<FakeInvocation> Invocations { get; } = new();

        public void OnRequestHeartbeat(Func<string, Task> handler)
        {
            Handlers["requestHeartbeat"] = handler;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCalls++;
            return Task.CompletedTask;
        }

        public Task InvokeAsync(string methodName, object?[] arguments, CancellationToken cancellationToken = default)
        {
            Invocations.Add(new FakeInvocation(methodName, arguments));
            return Task.CompletedTask;
        }
    }

    private sealed record FakeInvocation(string Method, object?[] Arguments);

    private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath)!);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Api.Sims4.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Sims4.SignalR")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}

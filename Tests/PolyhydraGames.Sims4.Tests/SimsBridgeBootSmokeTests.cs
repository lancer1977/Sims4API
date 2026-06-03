using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PolyhydraGames.Sims4.Bridge;

namespace PolyhydraGames.Sims4.Tests;

public sealed class SimsBridgeBootSmokeTests
{
    [Test]
    public async Task AddConfig_BindsSampleConfigAndResolvesBridge()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"sims4-boot-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);

        var configPath = Path.Combine(basePath, Setup.AppSettingsFileName);
        File.WriteAllText(configPath, """
            {
              "HubUrl": "https://localhost/signalr",
              "WebKey": "sample-web-key",
              "EventBufferPath": "buffer.jsonl",
              "RetryAttempts": 2,
              "RetryDelayMilliseconds": 75
            }
            """);

        var services = new ServiceCollection();
        services.AddConfig(basePath);
        services.AddSingleton<Connection>();

        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SimsBridgeOptions>>().Value;
        var bridge = provider.GetRequiredService<Connection>();

        Assert.That(options.HubUrl, Is.EqualTo("https://localhost/signalr"));
        Assert.That(options.WebKey, Is.EqualTo("sample-web-key"));
        Assert.That(options.EventBufferPath, Is.EqualTo("buffer.jsonl"));
        Assert.That(options.RetryAttempts, Is.EqualTo(2));
        Assert.That(options.RetryDelayMilliseconds, Is.EqualTo(75));
        Assert.That(bridge.WebKey, Is.EqualTo("sample-web-key"));
    }
}

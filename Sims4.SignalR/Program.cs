using PolyhydraGames.Sims4.Bridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AppDomain = System.AppDomain;
using Directory = System.IO.Directory;
using System.Reflection;

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5230");
builder.Services.AddConfig();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Connection>();
builder.Services.AddSingleton<SimsBridgeHostState>();

var app = builder.Build();

app.MapGet("/", () => Results.Json(new { surface = "Sims4.SignalR", status = "ok" }));
app.MapGet("/healthz", (SimsBridgeHostState state, IOptions<SimsBridgeOptions> options) =>
    Results.Json(state.CreateHealthReport(options.Value)));
app.MapGet("/state", (SimsBridgeHostState state, IOptions<SimsBridgeOptions> options) =>
    Results.Json(state.CreateStateReport(options.Value)));
app.MapGet("/version", () => Results.Json(new
{
    status = "ok",
    surface = "Sims4.SignalR",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"
}));
app.MapHub<SimsBridgeHub>("/signalr");

app.Lifetime.ApplicationStarted.Register(() => _ = StartBridgeAsync(app.Services));

await app.RunAsync();

static async Task StartBridgeAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var state = scope.ServiceProvider.GetRequiredService<SimsBridgeHostState>();
    var bridge = scope.ServiceProvider.GetRequiredService<Connection>();

    try
    {
        state.MarkStarting();
        await bridge.StartAsync();
        state.MarkConnected();
    }
    catch (Exception ex)
    {
        state.MarkFailed(ex);
    }
}

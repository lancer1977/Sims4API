using Microsoft.AspNetCore.SignalR.Client;

string hubUrl = "https://channelcheevos.com/hubs/stream";
string streamerUserId = "<your-guid>"; // e.g., from config
string jwt = await GetDeviceTokenAsync(); // Client credentials or device code flow

var conn = new HubConnectionBuilder()
    .WithUrl($"{hubUrl}?streamerId={streamerUserId}", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(jwt)!;
    })
    .WithAutomaticReconnect()
    .Build();

// Optional: receive acks or commands from server
conn.On<string>("requestHeartbeat", async (_) =>
{
    await conn.InvokeAsync("SubmitEvent", new StreamEvent(
        streamerUserId, "Heartbeat", DateTimeOffset.UtcNow,
        new { status = "ok" }, Guid.NewGuid().ToString()));
});

await conn.StartAsync();

// Helper: send an event
async Task SendEventAsync(string type, object payload, string? id = null)
{
    var evt = new StreamEvent(
        streamerUserId, type, DateTimeOffset.UtcNow, payload, id ?? Guid.NewGuid().ToString());

    // Basic retry with local buffer
    for (int i = 0; i < 5; i++)
    {
        try { await conn.InvokeAsync("SubmitEvent", evt); return; }
        catch { await Task.Delay(500 * (i + 1)); }
    }
    // Fallback: write to a local queue file to flush later
    await File.AppendAllTextAsync("event-buffer.jsonl", System.Text.Json.JsonSerializer.Serialize(evt) + "\n");
}

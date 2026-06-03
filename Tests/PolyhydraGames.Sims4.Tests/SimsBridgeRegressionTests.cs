using System.Text.Json;
using PolyhydraGames.Sims4.Bridge;
using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class SimsBridgeRegressionTests
{
    [Test]
    public async Task BufferAppender_WritesJsonLineAndCanBeReadBack()
    {
        var bufferPath = CreateTempPath("buffer", "events.jsonl");
        var evt = new StreamEvent(
            StreamerUserId: "streamer-1",
            Type: SimsEventNames.CommandFailed,
            Timestamp: new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero),
            Payload: new { action = SimsActionNames.TakeItem, reason = "missing-item" },
            Id: "evt-1");

        await SimsBridgeBuffer.AppendAsync(evt, bufferPath);

        var lines = await File.ReadAllLinesAsync(bufferPath);
        Assert.That(lines, Has.Length.EqualTo(1));

        var roundTripped = JsonSerializer.Deserialize<StreamEvent>(lines[0]);
        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.StreamerUserId, Is.EqualTo(evt.StreamerUserId));
        Assert.That(roundTripped.Type, Is.EqualTo(evt.Type));
        Assert.That(roundTripped.Timestamp, Is.EqualTo(evt.Timestamp));
        Assert.That(roundTripped.Id, Is.EqualTo(evt.Id));

        var payload = AssertPayloadJson(roundTripped.Payload);
        Assert.That(payload.GetProperty("action").GetString(), Is.EqualTo(SimsActionNames.TakeItem));
        Assert.That(payload.GetProperty("reason").GetString(), Is.EqualTo("missing-item"));
    }

    [Test]
    public async Task BufferAppender_AppendsEachEventOnItsOwnLine()
    {
        var bufferPath = CreateTempPath("buffer", "append.jsonl");
        var first = new StreamEvent("streamer-1", SimsEventNames.Heartbeat, DateTimeOffset.UtcNow, new { status = "ok" }, "evt-1");
        var second = new StreamEvent("streamer-1", SimsEventNames.Capabilities, DateTimeOffset.UtcNow, new { supports = true }, "evt-2");

        await SimsBridgeBuffer.AppendAsync(first, bufferPath);
        await SimsBridgeBuffer.AppendAsync(second, bufferPath);

        var lines = await File.ReadAllLinesAsync(bufferPath);
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[0], Does.Contain("Heartbeat"));
        Assert.That(lines[1], Does.Contain("Capabilities"));
    }

    [Test]
    public void SimsCommand_SerializesWithStableBridgeShape()
    {
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.AddItem,
            Timestamp: new DateTimeOffset(2026, 5, 31, 13, 5, 0, TimeSpan.Zero),
            Payload: new { itemId = "item-1", quantity = 2 },
            Id: "cmd-1",
            Target: new SimsTarget(SimId: "sim-42", InventoryScope: "household"),
            CorrelationId: "corr-77");

        var json = JsonSerializer.Serialize(command);
        using var doc = JsonDocument.Parse(json);

        Assert.That(doc.RootElement.GetProperty("Action").GetString(), Is.EqualTo(SimsActionNames.AddItem));
        Assert.That(doc.RootElement.GetProperty("Id").GetString(), Is.EqualTo("cmd-1"));
        Assert.That(doc.RootElement.GetProperty("CorrelationId").GetString(), Is.EqualTo("corr-77"));
        Assert.That(doc.RootElement.GetProperty("Target").GetProperty("SimId").GetString(), Is.EqualTo("sim-42"));
        Assert.That(doc.RootElement.GetProperty("Payload").GetProperty("itemId").GetString(), Is.EqualTo("item-1"));
        Assert.That(doc.RootElement.GetProperty("Payload").GetProperty("quantity").GetInt32(), Is.EqualTo(2));
    }

    private static string CreateTempPath(string subdir, string fileName)
    {
        var root = Path.Combine(Path.GetTempPath(), "Api.Sims4.Tests", subdir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, fileName);
    }

    private static JsonElement AssertPayloadJson(object payload)
    {
        Assert.That(payload, Is.TypeOf<JsonElement>());
        return (JsonElement)payload;
    }
}

using System.Text.Json;
using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Tests;

public sealed class SimsModRuntimeTests
{
    [Test]
    public async Task ProcessPendingCommandsAsync_ReadsQueueAndDispatchesInventoryHandlers()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"sims4-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);
        var queuePath = Path.Combine(basePath, "command-queue.jsonl");
        var journalPath = Path.Combine(basePath, "command-status.jsonl");
        var store = new SimsInventoryStore();
        var dispatcher = new SimsCommandDispatcher(new ISimsCommandHandler[]
        {
            new SimsAddItemHandler(store),
            new SimsTakeItemHandler(store),
            new SimsTakeStockHandler(store),
        }, journalPath);
        var runtime = new SimsModRuntime(dispatcher, queuePath);

        var addCommand = new SimsCommand(
            "streamer-1",
            SimsActionNames.AddItem,
            DateTimeOffset.Parse("2026-05-31T12:00:00+00:00"),
            new SimsAddItemRequest("item-1", "Laser Cutter", 2, false, "Debug", "Spawner"),
            "cmd-1",
            new SimsTarget(HouseholdId: "household-9", InventoryScope: "household"));

        var stocktakeCommand = new SimsCommand(
            "streamer-1",
            SimsActionNames.TakeStock,
            DateTimeOffset.Parse("2026-05-31T12:01:00+00:00"),
            new SimsTakeStockRequest("phase-check"),
            "cmd-2",
            new SimsTarget(HouseholdId: "household-9", InventoryScope: "household"));

        await File.WriteAllTextAsync(queuePath, string.Join(Environment.NewLine,
            JsonSerializer.Serialize(addCommand),
            JsonSerializer.Serialize(stocktakeCommand)) + Environment.NewLine);

        var results = await runtime.ProcessPendingCommandsAsync();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(results[1].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(store.LastSnapshot, Is.Not.Null);
        Assert.That(store.LastSnapshot!.Notes, Is.EqualTo("phase-check"));
        Assert.That(store.LastSnapshot.Items, Has.Count.EqualTo(1));
        Assert.That(store.LastSnapshot.Items[0].DisplayName, Is.EqualTo("Laser Cutter"));
        Assert.That(File.Exists(journalPath), Is.True);
    }

    [Test]
    public async Task ProcessPendingCommandsAsync_ConsumesEachQueuedCommandAtMostOnceAcrossRuns()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"sims4-runtime-once-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);
        var queuePath = Path.Combine(basePath, "command-queue.jsonl");
        var journalPath = Path.Combine(basePath, "command-status.jsonl");
        var store = new SimsInventoryStore();
        var dispatcher = new SimsCommandDispatcher(new ISimsCommandHandler[]
        {
            new SimsAddItemHandler(store),
        }, journalPath);
        var runtime = new SimsModRuntime(dispatcher, queuePath);

        var command = new SimsCommand(
            "streamer-1",
            SimsActionNames.AddItem,
            DateTimeOffset.Parse("2026-05-31T12:10:00+00:00"),
            new SimsAddItemRequest("item-9", "Moon Rock", 1, false, "Debug", "Spawner"),
            "cmd-once",
            new SimsTarget(HouseholdId: "household-9", InventoryScope: "household"));

        await File.WriteAllTextAsync(queuePath, JsonSerializer.Serialize(command) + Environment.NewLine);

        var firstRun = await runtime.ProcessPendingCommandsAsync();
        var secondRun = await runtime.ProcessPendingCommandsAsync();
        var entries = await File.ReadAllLinesAsync(journalPath);

        Assert.That(firstRun, Has.Count.EqualTo(1));
        Assert.That(firstRun[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(secondRun, Is.Empty);
        Assert.That(File.Exists(queuePath), Is.False);
        Assert.That(entries, Has.Length.EqualTo(3));
    }

    [Test]
    public async Task ProcessPendingCommandsAsync_ReadsQueueAndDispatchesWorldHandlers()
    {
        var basePath = Path.Combine(Path.GetTempPath(), $"sims4-runtime-world-{Guid.NewGuid():N}");
        Directory.CreateDirectory(basePath);
        var queuePath = Path.Combine(basePath, "command-queue.jsonl");
        var journalPath = Path.Combine(basePath, "command-status.jsonl");
        var store = new SimsWorldActionStore();
        var dispatcher = new SimsCommandDispatcher(new ISimsCommandHandler[]
        {
            new SimsRunInteractionHandler(store),
            new SimsSpawnObjectHandler(store),
        }, journalPath);
        var runtime = new SimsModRuntime(dispatcher, queuePath);

        var interactionCommand = new SimsCommand(
            "streamer-1",
            SimsActionNames.RunInteraction,
            DateTimeOffset.Parse("2026-05-31T12:02:00+00:00"),
            new SimsRunInteractionRequest(DateTimeOffset.Parse("2026-05-31T12:02:00+00:00"), "interaction-42", "Friendly Chat", new SimsTarget(SimId: "sim-42"), "stream redeem"),
            "cmd-10",
            new SimsTarget(SimId: "sim-42"));

        var spawnCommand = new SimsCommand(
            "streamer-1",
            SimsActionNames.SpawnObject,
            DateTimeOffset.Parse("2026-05-31T12:03:00+00:00"),
            new SimsSpawnObjectRequest(DateTimeOffset.Parse("2026-05-31T12:03:00+00:00"), "object-9", "Laser Cutter", new SimsTarget(HouseholdId: "household-9"), 2, "kitchen counter", "stream gift"),
            "cmd-11",
            new SimsTarget(HouseholdId: "household-9"));

        await File.WriteAllTextAsync(queuePath, string.Join(Environment.NewLine,
            JsonSerializer.Serialize(interactionCommand),
            JsonSerializer.Serialize(spawnCommand)) + Environment.NewLine);

        var results = await runtime.ProcessPendingCommandsAsync();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(results[1].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(store.Actions, Has.Count.EqualTo(2));
        Assert.That(store.LastInteraction, Is.Not.Null);
        Assert.That(store.LastInteraction!.PrimaryId, Is.EqualTo("interaction-42"));
        Assert.That(store.LastSpawnedObject, Is.Not.Null);
        Assert.That(store.LastSpawnedObject!.Quantity, Is.EqualTo(2));
        Assert.That(store.LastSpawnedObject.DisplayName, Is.EqualTo("Laser Cutter"));
        Assert.That(File.Exists(journalPath), Is.True);
    }

    [Test]
    public async Task QueueReader_IgnoresBlankLinesAndRoundTripsCommands()
    {
        var queuePath = Path.Combine(Path.GetTempPath(), $"sims4-queue-{Guid.NewGuid():N}", "command-queue.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(queuePath)!);
        var command = new SimsCommand(
            "streamer-1",
            SimsActionNames.TakeItem,
            DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"),
            new SimsTakeItemRequest("item-1", 1, "use up"),
            "cmd-3",
            new SimsTarget(SimId: "sim-42", InventoryScope: "personal"));

        await File.WriteAllTextAsync(queuePath, $"\n{JsonSerializer.Serialize(command)}\n");

        var commands = await SimsCommandQueueReader.ReadAsync(queuePath);

        Assert.That(commands, Has.Count.EqualTo(1));
        Assert.That(commands[0].StreamerUserId, Is.EqualTo(command.StreamerUserId));
        Assert.That(commands[0].Action, Is.EqualTo(command.Action));
        Assert.That(commands[0].Target, Is.EqualTo(command.Target));
        Assert.That(commands[0].Id, Is.EqualTo(command.Id));

        Assert.That(commands[0].Payload, Is.TypeOf<JsonElement>());
        var payload = (JsonElement)commands[0].Payload;
        Assert.That(payload.GetProperty("ItemId").GetString(), Is.EqualTo("item-1"));
        Assert.That(payload.GetProperty("Quantity").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public void InventoryStore_AddTakeAndSnapshotShareTheSameScope()
    {
        var store = new SimsInventoryStore();
        var target = new SimsTarget(HouseholdId: "household-9", InventoryScope: "household");

        var addResult = store.AddItem("streamer-1", target, new SimsAddItemRequest("item-1", "Laser Cutter", 3, false, "Debug", "Spawner"));
        var removeResult = store.TakeItem("streamer-1", target, new SimsTakeItemRequest("item-1", 1, "consumed"));
        var snapshot = store.CaptureSnapshot("streamer-1", target, "stocktake");

        Assert.That(addResult.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(removeResult.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(snapshot.Items, Has.Count.EqualTo(1));
        Assert.That(snapshot.Items[0].Quantity, Is.EqualTo(2));
        Assert.That(snapshot.Items[0].DisplayName, Is.EqualTo("Laser Cutter"));
        Assert.That(snapshot.Notes, Is.EqualTo("stocktake"));
    }

    [Test]
    public void InventoryStore_RejectsInvalidOrMissingItems()
    {
        var store = new SimsInventoryStore();

        var badAdd = store.AddItem("streamer-1", null, new SimsAddItemRequest("item-0", "", 0));
        var missingTake = store.TakeItem("streamer-1", null, new SimsTakeItemRequest("item-99", 1));

        Assert.That(badAdd.Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(badAdd.Message, Does.Contain("Quantity"));
        Assert.That(missingTake.Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(missingTake.Message, Does.Contain("not available"));
    }

    [Test]
    public void WorldActionStore_RejectsInvalidRequests()
    {
        var store = new SimsWorldActionStore();

        var badInteraction = store.RunInteraction("streamer-1", null, new SimsRunInteractionRequest(DateTimeOffset.Parse("2026-05-31T12:00:00+00:00"), ""));
        var badSpawn = store.SpawnObject("streamer-1", null, new SimsSpawnObjectRequest(DateTimeOffset.Parse("2026-05-31T12:00:00+00:00"), "object-9", "", Quantity: 0));

        Assert.That(badInteraction.Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(badInteraction.Message, Does.Contain("InteractionId"));
        Assert.That(badSpawn.Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(badSpawn.Message, Does.Contain("Quantity"));
    }
}

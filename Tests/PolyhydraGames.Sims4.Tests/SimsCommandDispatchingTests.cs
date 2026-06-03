using System.Text.Json;

using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Tests;

public sealed class SimsCommandDispatchingTests
{
    [Test]
    public async Task DispatchAsync_RecordsCommandLifecycleForProcessedCommands()
    {
        var journalPath = Path.Combine(Path.GetTempPath(), $"sims4-dispatch-{Guid.NewGuid():N}", "status.jsonl");
        var handler = new FakeHandler(SimsActionNames.AddItem, SimsCommandHandlerResult.Processed("queued"));
        var dispatcher = new SimsCommandDispatcher([handler], journalPath);
        var command = new SimsCommand("streamer-1", SimsActionNames.AddItem, DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"), new { itemId = "item-1", quantity = 2 }, "cmd-1");

        var record = await dispatcher.DispatchAsync(command);

        var entries = await ReadJournalAsync(journalPath);

        Assert.That(handler.WasCalled, Is.True);
        Assert.That(record.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(record.HandlerName, Does.Contain(nameof(FakeHandler)));
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(entries[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Pending));
        Assert.That(entries[1].Status, Is.EqualTo(SimsCommandDispatchStatus.Processing));
        Assert.That(entries[2].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(entries[1].ProcessingAt, Is.Not.Null);
        Assert.That(entries[2].ProcessedAt, Is.Not.Null);
        Assert.That(entries[2].Message, Does.Contain("queued"));
    }

    [Test]
    public async Task DispatchAsync_RecordsUnhandledCommandsWithMetadata()
    {
        var journalPath = Path.Combine(Path.GetTempPath(), $"sims4-dispatch-{Guid.NewGuid():N}", "status.jsonl");
        var dispatcher = new SimsCommandDispatcher(Array.Empty<ISimsCommandHandler>(), journalPath);
        var command = new SimsCommand("streamer-1", SimsActionNames.TakeStock, DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"), new { }, "cmd-2");

        var record = await dispatcher.DispatchAsync(command);
        var entries = await ReadJournalAsync(journalPath);

        Assert.That(record.Status, Is.EqualTo(SimsCommandDispatchStatus.Unhandled));
        Assert.That(record.FailureCode, Is.EqualTo("NO_HANDLER"));
        Assert.That(record.Message, Does.Contain(SimsActionNames.TakeStock));
        Assert.That(record.RawErrorPayload, Is.EqualTo("{}"));
        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[1].Status, Is.EqualTo(SimsCommandDispatchStatus.Unhandled));
        Assert.That(entries[1].FailureReason, Does.Contain(SimsActionNames.TakeStock));
    }

    [Test]
    public async Task DispatchAsync_RecordsHandlerFailuresWithFailureMetadata()
    {
        var journalPath = Path.Combine(Path.GetTempPath(), $"sims4-dispatch-{Guid.NewGuid():N}", "status.jsonl");
        var handler = new FakeHandler(SimsActionNames.TakeItem, SimsCommandHandlerResult.Failed("should not be returned"))
        {
            ThrowOnHandle = true,
        };
        var dispatcher = new SimsCommandDispatcher([handler], journalPath);
        var command = new SimsCommand("streamer-1", SimsActionNames.TakeItem, DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"), new { itemId = "item-2" }, "cmd-3");

        var record = await dispatcher.DispatchAsync(command);

        Assert.That(record.Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(record.FailureCode, Is.EqualTo("HANDLER_EXCEPTION"));
        Assert.That(record.FailureStack, Does.Contain("InvalidOperationException"));
        Assert.That(record.Message, Does.Contain("boom"));
        Assert.That(record.RawErrorPayload, Does.Contain("\"itemId\""));
        Assert.That(record.RawErrorPayload, Does.Contain("item-2"));

        var entries = await ReadJournalAsync(journalPath);
        Assert.That(entries, Has.Count.EqualTo(3));
        Assert.That(entries[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Pending));
        Assert.That(entries[1].Status, Is.EqualTo(SimsCommandDispatchStatus.Processing));
        Assert.That(entries[2].Status, Is.EqualTo(SimsCommandDispatchStatus.Failed));
        Assert.That(entries[2].FailureCode, Is.EqualTo("HANDLER_EXCEPTION"));
        Assert.That(entries[2].FailureReason, Does.Contain("boom"));
        Assert.That(entries[2].FailureStack, Does.Contain("InvalidOperationException"));
        Assert.That(entries[2].RawErrorPayload, Does.Contain("\"itemId\""));
        Assert.That(entries[2].RawErrorPayload, Does.Contain("item-2"));
    }

    [Test]
    public async Task DispatchAsync_ProvidesProcessedAndFailedHistoryQueries()
    {
        var journalPath = Path.Combine(Path.GetTempPath(), $"sims4-dispatch-{Guid.NewGuid():N}", "status.jsonl");
        var successHandler = new FakeHandler(SimsActionNames.AddItem, SimsCommandHandlerResult.Processed("queued"));
        var failedHandler = new FakeHandler(SimsActionNames.TakeItem, SimsCommandHandlerResult.Failed("not enough"));

        var dispatcher = new SimsCommandDispatcher([successHandler, failedHandler], journalPath);

        await dispatcher.DispatchAsync(new SimsCommand("streamer-1", SimsActionNames.AddItem, DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"), new { itemId = "item-1" }, "cmd-ok"));
        await dispatcher.DispatchAsync(new SimsCommand("streamer-1", SimsActionNames.TakeItem, DateTimeOffset.Parse("2026-05-31T12:06:00+00:00"), new { itemId = "item-2" }, "cmd-fail"));

        var repository = new SimsCommandDispatchJournal(journalPath);
        var processedHistory = await repository.ReadProcessedHistoryAsync();
        var failedHistory = await repository.ReadFailedHistoryAsync();

        Assert.That(processedHistory, Has.Count.EqualTo(1));
        Assert.That(processedHistory[0].CommandId, Is.EqualTo("cmd-ok"));
        Assert.That(failedHistory, Has.Count.EqualTo(1));
        Assert.That(failedHistory[0].CommandId, Is.EqualTo("cmd-fail"));
    }

    [Test]
    public async Task DispatchJournal_TryTransitionAsync_RequiresTheExpectedCurrentStatus()
    {
        var journalPath = Path.Combine(Path.GetTempPath(), $"sims4-dispatch-{Guid.NewGuid():N}", "status.jsonl");
        var journal = new SimsCommandDispatchJournal(journalPath);
        var pending = new SimsCommandDispatchRecord(
            CommandId: "cmd-atomic",
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.AddItem,
            CommandTimestamp: DateTimeOffset.Parse("2026-05-31T12:05:00+00:00"),
            DispatchedAt: DateTimeOffset.Parse("2026-05-31T12:05:03+00:00"),
            Status: SimsCommandDispatchStatus.Pending);

        await journal.AppendAsync(pending);

        var processing = await journal.TryTransitionAsync(
            commandId: pending.CommandId,
            expectedCurrentStatus: SimsCommandDispatchStatus.Pending,
            status: SimsCommandDispatchStatus.Processing,
            handlerName: "FakeHandler",
            message: "Processing command.");

        Assert.That(processing, Is.Not.Null);
        Assert.That(processing!.Status, Is.EqualTo(SimsCommandDispatchStatus.Processing));
        Assert.That(processing.ProcessingAt, Is.Not.Null);

        var rejected = await journal.TryTransitionAsync(
            commandId: pending.CommandId,
            expectedCurrentStatus: SimsCommandDispatchStatus.Pending,
            status: SimsCommandDispatchStatus.Processed,
            message: "This should not win.");

        var entries = await ReadJournalAsync(journalPath);

        Assert.That(rejected, Is.Null);
        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries[^1].Status, Is.EqualTo(SimsCommandDispatchStatus.Processing));
    }

    private static async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadJournalAsync(string journalPath)
    {
        var lines = await File.ReadAllLinesAsync(journalPath);
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<SimsCommandDispatchRecord>(line)!)
            .ToList();
    }

    private sealed class FakeHandler(string action, SimsCommandHandlerResult result) : ISimsCommandHandler
    {
        public bool ThrowOnHandle { get; init; }

        public bool WasCalled { get; private set; }

        public bool CanHandle(SimsCommand command) => command.Action == action;

        public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            if (ThrowOnHandle)
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult(result);
        }
    }
}

using System.Text.Json;
using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class SimsInfluenceSmokeTests
{
    [Test]
    public async Task Runtime_ProcessesQueuedSurpriseGuestInfluenceEndToEnd()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"sims-influence-smoke-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        var queuePath = Path.Combine(tempRoot, "command-queue.jsonl");
        var journalPath = Path.Combine(tempRoot, "command-status.jsonl");
        var timestamp = new DateTimeOffset(2026, 5, 31, 12, 30, 0, TimeSpan.Zero);

        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TriggerInfluence,
            Timestamp: timestamp,
            Payload: new SimsSurpriseGuestRequest(
                RequestedAt: timestamp,
                Reason: "community redeem",
                Target: new SimsTarget(HouseholdId: "household-9"),
                GuestVariant: SimsGuestVariants.Friend,
                GuestName: "Alex",
                Venue: "front porch",
                AnnounceArrival: false),
            Id: "cmd-influence-smoke-1");

        await File.WriteAllTextAsync(queuePath, JsonSerializer.Serialize(command) + Environment.NewLine);

        try
        {
            var gate = new SimsInfluenceGate(utcNow: () => timestamp);
            var dispatcher = new SimsCommandDispatcher(new ISimsCommandHandler[] { new SimsTriggerInfluenceHandler(gate) }, journalPath);
            var runtime = new SimsModRuntime(dispatcher, queuePath);

            var records = await runtime.ProcessPendingCommandsAsync();

            Assert.That(records, Has.Count.EqualTo(1));
            Assert.That(records[0].CommandId, Is.EqualTo("cmd-influence-smoke-1"));
            Assert.That(records[0].Action, Is.EqualTo(SimsActionNames.TriggerInfluence));
            Assert.That(records[0].Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
            Assert.That(records[0].Message, Does.Contain("Alex (friendly) shows up unexpectedly at front porch."));
            Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
            Assert.That(gate.AuditTrail[0].Kind, Is.EqualTo(SimsInfluenceKinds.SurpriseGuest));

            var journalLines = await File.ReadAllLinesAsync(journalPath);
            Assert.That(journalLines, Is.Not.Empty);
            Assert.That(journalLines[^1], Does.Contain("\"Status\":\"Processed\""));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

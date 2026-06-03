using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class SimsInfluenceGateTests
{
    [Test]
    public void Gate_RejectsFireWithoutApprovalAndRecordsAudit()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(utcNow: () => moment);

        var decision = gate.Evaluate(
            "streamer-1",
            new SimsInfluenceRequest(
                SimsInfluenceKinds.FireIncident,
                moment,
                Reason: "chat redeem"));

        Assert.That(decision.Allowed, Is.False);
        Assert.That(decision.RejectionReason, Does.Contain("requires approval"));
        Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
        Assert.That(gate.AuditTrail[0].Kind, Is.EqualTo(SimsInfluenceKinds.FireIncident));
    }

    [Test]
    public void Gate_AllowsApprovedKind_ThenHonorsCooldown()
    {
        var current = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(cooldown: TimeSpan.FromMinutes(10), utcNow: () => current);

        var approved = gate.Evaluate(
            "streamer-1",
            new SimsInfluenceRequest(
                SimsInfluenceKinds.FireIncident,
                current,
                Reason: "approved by operator",
                RequiresApproval: true,
                ApprovalToken: "ok"));

        Assert.That(approved.Allowed, Is.True);
        Assert.That(approved.Message, Does.Contain("Approved influence 'fire_incident'"));

        current = current.AddMinutes(1);
        var coolingDown = gate.Evaluate(
            "streamer-1",
            new SimsInfluenceRequest(
                SimsInfluenceKinds.FireIncident,
                current,
                RequiresApproval: true,
                ApprovalToken: "ok"));

        Assert.That(coolingDown.Allowed, Is.False);
        Assert.That(coolingDown.CooldownUntil, Is.Not.Null);
        Assert.That(coolingDown.RejectionReason, Does.Contain("cooling down"));
    }

    [Test]
    public void Gate_RejectsUnknownInfluenceKinds()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(utcNow: () => moment);

        var decision = gate.Evaluate(
            "streamer-1",
            new SimsInfluenceRequest("space_alien", moment));

        Assert.That(decision.Allowed, Is.False);
        Assert.That(decision.RejectionReason, Does.Contain("not enabled"));
    }

    [Test]
    public async Task Handler_ProcessesSurpriseGuestCommand_WithGenericPayload()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(cooldown: TimeSpan.FromMinutes(10), utcNow: () => moment);
        var handler = new SimsTriggerInfluenceHandler(gate);
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TriggerInfluence,
            Timestamp: moment,
            Payload: new SimsInfluenceRequest(
                SimsInfluenceKinds.SurpriseGuest,
                moment,
                Reason: "party time"),
            Id: "cmd-influence-1");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(result.Message, Does.Contain("a surprise guest"));
        Assert.That(result.Message, Does.Contain("random"));
        Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Handler_ProcessesFireIncidentCommandWithSpecializedPayload()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(cooldown: TimeSpan.FromMinutes(10), utcNow: () => moment);
        var handler = new SimsTriggerInfluenceHandler(gate);
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TriggerInfluence,
            Timestamp: moment,
            Payload: new SimsFireIncidentRequest(
                RequestedAt: moment,
                Reason: "hot kitchen",
                Target: new SimsTarget(ObjectId: "stove-9"),
                ApprovalToken: "approved",
                Room: "kitchen",
                Severity: "high",
                NotifyFirefighters: false),
            Id: "cmd-fire-1");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(result.Message, Does.Contain("Fire incident staged for kitchen at high severity."));
        Assert.That(result.Message, Does.Contain("Firefighters not notified."));
        Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
        Assert.That(gate.AuditTrail[0].Kind, Is.EqualTo(SimsInfluenceKinds.FireIncident));
    }

    [Test]
    public async Task Handler_ProcessesRobberBreakInCommand()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 1, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(cooldown: TimeSpan.FromMinutes(10), utcNow: () => moment);
        var handler = new SimsTriggerInfluenceHandler(gate);
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TriggerInfluence,
            Timestamp: moment,
            Payload: new SimsRobberBreakInRequest(
                RequestedAt: moment,
                Reason: "chat redeem",
                Target: new SimsTarget(HouseholdId: "household-9"),
                ApprovalToken: "approved",
                EntryPoint: "back window",
                Severity: "high",
                Stealthy: false,
                PoliceNotified: false),
            Id: "cmd-robber-1");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(result.Message, Does.Contain("Robber break-in staged via back window at high severity in loud mode."));
        Assert.That(result.Message, Does.Contain("Police not notified."));
        Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
        Assert.That(gate.AuditTrail[0].Kind, Is.EqualTo(SimsInfluenceKinds.RobberBreakIn));
    }

    [Test]
    public async Task Handler_ProcessesSurpriseGuestCommand()
    {
        var moment = new DateTimeOffset(2026, 5, 31, 12, 2, 0, TimeSpan.Zero);
        var gate = new SimsInfluenceGate(cooldown: TimeSpan.FromMinutes(10), utcNow: () => moment);
        var handler = new SimsTriggerInfluenceHandler(gate);
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TriggerInfluence,
            Timestamp: moment,
            Payload: new SimsSurpriseGuestRequest(
                RequestedAt: moment,
                Reason: "chat redeem",
                Target: new SimsTarget(HouseholdId: "household-9"),
                GuestVariant: SimsGuestVariants.Neighbor,
                GuestName: "Mrs. Crumplebottom",
                Venue: "porch",
                AnnounceArrival: true),
            Id: "cmd-guest-1");

        var result = await handler.HandleAsync(command);

        Assert.That(result.Status, Is.EqualTo(SimsCommandDispatchStatus.Processed));
        Assert.That(result.Message, Does.Contain("Mrs. Crumplebottom (neighbor) arrives with a knock at porch."));
        Assert.That(gate.AuditTrail, Has.Count.EqualTo(1));
        Assert.That(gate.AuditTrail[0].Kind, Is.EqualTo(SimsInfluenceKinds.SurpriseGuest));
    }
}

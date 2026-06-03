using System.Text.Json;
using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class SimsContractSerializationTests
{
    [Test]
    public void StreamEvent_RoundTripsOpaquePayload()
    {
        var original = new StreamEvent(
            StreamerUserId: "streamer-1",
            Type: SimsEventNames.CommandQueued,
            Timestamp: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            Payload: new { action = SimsActionNames.TakeStock, count = 3 },
            Id: "evt-1");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(roundTripped.Type, Is.EqualTo(original.Type));
        Assert.That(roundTripped.Timestamp, Is.EqualTo(original.Timestamp));
        Assert.That(roundTripped.Id, Is.EqualTo(original.Id));

        var payload = AssertPayloadJson(roundTripped.Payload);
        Assert.That(payload.GetProperty("action").GetString(), Is.EqualTo(SimsActionNames.TakeStock));
        Assert.That(payload.GetProperty("count").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public void SimsCommand_RoundTripsTargetAndCorrelationId()
    {
        var original = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.AddItem,
            Timestamp: new DateTimeOffset(2026, 5, 31, 12, 5, 0, TimeSpan.Zero),
            Payload: new { itemId = "item-1", quantity = 2 },
            Id: "cmd-1",
            Target: new SimsTarget(SimId: "sim-42", InventoryScope: "household"),
            CorrelationId: "corr-77");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(roundTripped.Action, Is.EqualTo(original.Action));
        Assert.That(roundTripped.Timestamp, Is.EqualTo(original.Timestamp));
        Assert.That(roundTripped.Id, Is.EqualTo(original.Id));
        Assert.That(roundTripped.CorrelationId, Is.EqualTo(original.CorrelationId));
        Assert.That(roundTripped.Target, Is.Not.Null);
        Assert.That(roundTripped.Target!.SimId, Is.EqualTo("sim-42"));
        Assert.That(roundTripped.Target.InventoryScope, Is.EqualTo("household"));

        var payload = AssertPayloadJson(roundTripped.Payload);
        Assert.That(payload.GetProperty("itemId").GetString(), Is.EqualTo("item-1"));
        Assert.That(payload.GetProperty("quantity").GetInt32(), Is.EqualTo(2));
    }

    [Test]
    public void SimsCommandDispatchRecord_RoundTripsStatusJournalShape()
    {
        var original = new SimsCommandDispatchRecord(
            CommandId: "cmd-1",
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.AddItem,
            CommandTimestamp: new DateTimeOffset(2026, 5, 31, 12, 5, 0, TimeSpan.Zero),
            DispatchedAt: new DateTimeOffset(2026, 5, 31, 12, 5, 3, TimeSpan.Zero),
            Status: SimsCommandDispatchStatus.Processed,
            HandlerName: "FakeHandler",
            Message: "queued",
            Target: new SimsTarget(SimId: "sim-42", InventoryScope: "household"),
            CorrelationId: "corr-77");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsCommandHandlerResult_RoundTripsDispatchOutcome()
    {
        var original = SimsCommandHandlerResult.Failed("boom");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsAddItemRequest_RoundTripsPayloadShape()
    {
        var original = new SimsAddItemRequest("item-1", "Laser Cutter", 3, false, "Debug", "Spawner");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsTakeItemRequest_RoundTripsPayloadShape()
    {
        var original = new SimsTakeItemRequest("item-1", 1, "used");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsTakeStockRequest_RoundTripsPayloadShape()
    {
        var original = new SimsTakeStockRequest("phase-check");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsRunInteractionRequest_RoundTripsPayloadShape()
    {
        var original = new SimsRunInteractionRequest(
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:06:00Z"),
            InteractionId: "interaction-42",
            InteractionName: "Friendly Chat",
            Target: new SimsTarget(SimId: "sim-42"),
            QueueContext: "stream redeem");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsSpawnObjectRequest_RoundTripsPayloadShape()
    {
        var original = new SimsSpawnObjectRequest(
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:07:00Z"),
            ObjectId: "object-9",
            DisplayName: "Laser Cutter",
            Target: new SimsTarget(HouseholdId: "household-9"),
            Quantity: 2,
            Placement: "kitchen counter",
            Notes: "stream gift");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsWorldActionRecord_RoundTripsAuditPayload()
    {
        var original = new SimsWorldActionRecord(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.RunInteraction,
            PrimaryId: "interaction-42",
            DisplayName: "Friendly Chat",
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:08:00Z"),
            Target: new SimsTarget(SimId: "sim-42"),
            Quantity: 1,
            Placement: "living room",
            Notes: "stream redeem");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void InfluenceRequest_RoundTripsAllFields()
    {
        var original = new SimsInfluenceRequest(
            Kind: SimsInfluenceKinds.SurpriseGuest,
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:00:00Z"),
            Reason: "chat redeem",
            Target: new SimsTarget(HouseholdId: "house-7"),
            RequiresApproval: true,
            ApprovalToken: "approved");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void FireIncidentRequest_RoundTripsSpecializedFields()
    {
        var original = new SimsFireIncidentRequest(
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:00:00Z"),
            Reason: "chat redeem",
            Target: new SimsTarget(ObjectId: "stove-9"),
            ApprovalToken: "approved",
            Room: "kitchen",
            Severity: "high",
            NotifyFirefighters: false);

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
        Assert.That(roundTripped.Kind, Is.EqualTo(SimsInfluenceKinds.FireIncident));
    }

    [Test]
    public void RobberBreakInRequest_RoundTripsSpecializedFields()
    {
        var original = new SimsRobberBreakInRequest(
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:01:00Z"),
            Reason: "chat redeem",
            Target: new SimsTarget(HouseholdId: "household-9"),
            ApprovalToken: "approved",
            EntryPoint: "back window",
            Severity: "high",
            Stealthy: false,
            PoliceNotified: false);

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
        Assert.That(roundTripped.Kind, Is.EqualTo(SimsInfluenceKinds.RobberBreakIn));
    }

    [Test]
    public void SurpriseGuestRequest_RoundTripsSpecializedFields()
    {
        var original = new SimsSurpriseGuestRequest(
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:02:00Z"),
            Reason: "chat redeem",
            Target: new SimsTarget(HouseholdId: "household-9"),
            GuestVariant: SimsGuestVariants.Rival,
            GuestName: "Morgyn Ember",
            Venue: "living room",
            AnnounceArrival: false);

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
        Assert.That(roundTripped.Kind, Is.EqualTo(SimsInfluenceKinds.SurpriseGuest));
        Assert.That(roundTripped.GuestVariant, Is.EqualTo(SimsGuestVariants.Rival));
    }

    [Test]
    public void InfluenceDecision_RoundTripsAuditDetails()
    {
        var original = new SimsInfluenceDecision(
            StreamerUserId: "streamer-1",
            Kind: SimsInfluenceKinds.FireIncident,
            RequestedAt: DateTimeOffset.Parse("2026-05-31T12:03:00Z"),
            EvaluatedAt: DateTimeOffset.Parse("2026-05-31T12:04:00Z"),
            Allowed: false,
            Message: "Influence kind 'fire_incident' is cooling down until 2026-05-31T12:10:00.0000000+00:00.",
            RejectionReason: "Influence kind 'fire_incident' is cooling down until 2026-05-31T12:10:00.0000000+00:00.",
            CooldownUntil: DateTimeOffset.Parse("2026-05-31T12:10:00Z"),
            Target: new SimsTarget(HouseholdId: "household-9"),
            Reason: "chat redeem");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
        Assert.That(roundTripped.Kind, Is.EqualTo(SimsInfluenceKinds.FireIncident));
        Assert.That(roundTripped.Allowed, Is.False);
        Assert.That(roundTripped.CooldownUntil, Is.Not.Null);
    }

    [Test]
    public void SimsInventoryItem_RoundTripsDetails()
    {
        var original = new SimsInventoryItem(
            ItemId: "item-1",
            DisplayName: "Laser Cutter",
            Quantity: 1,
            IsStackable: false,
            Category: "Debug",
            Source: "Spawner");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void SimsInventorySnapshot_RoundTripsTargetItemsAndFunds()
    {
        var original = new SimsInventorySnapshot(
            StreamerUserId: "streamer-1",
            CapturedAt: new DateTimeOffset(2026, 5, 31, 12, 10, 0, TimeSpan.Zero),
            Target: new SimsTarget(HouseholdId: "household-9", InventoryScope: "household"),
            Items: new[]
            {
                new SimsInventoryItem("item-1", "Laser Cutter", 1, false, "Debug", "Spawner"),
                new SimsInventoryItem("item-2", "Clay", 5, true, "Collectible", "Harvest"),
            },
            HouseholdFunds: 1234,
            Notes: "stocktake");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(roundTripped.CapturedAt, Is.EqualTo(original.CapturedAt));
        Assert.That(roundTripped.Target, Is.Not.Null);
        Assert.That(roundTripped.Target!.HouseholdId, Is.EqualTo("household-9"));
        Assert.That(roundTripped.Target.InventoryScope, Is.EqualTo("household"));
        Assert.That(roundTripped.HouseholdFunds, Is.EqualTo(1234));
        Assert.That(roundTripped.Notes, Is.EqualTo("stocktake"));
        Assert.That(roundTripped.Items, Has.Count.EqualTo(2));
        Assert.That(roundTripped.Items[0].DisplayName, Is.EqualTo("Laser Cutter"));
    }

    [Test]
    public void SimsModCapabilities_RoundTripsSupportedSurface()
    {
        var original = new SimsModCapabilities(
            ModVersion: "1.2.3",
            ApiVersion: "v1",
            SupportedActions: SimsActionNames.All,
            SupportedEvents: SimsEventNames.All,
            SupportsInventoryExposure: true,
            SupportsStocktake: true);

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped.ModVersion, Is.EqualTo(original.ModVersion));
        Assert.That(roundTripped.ApiVersion, Is.EqualTo(original.ApiVersion));
        Assert.That(roundTripped.SupportedActions, Does.Contain(SimsActionNames.TakeStock));
        Assert.That(roundTripped.SupportedEvents, Does.Contain(SimsEventNames.InventorySnapshot));
        Assert.That(roundTripped.SupportsInventoryExposure, Is.True);
        Assert.That(roundTripped.SupportsStocktake, Is.True);
    }

    [Test]
    public void SimsTarget_RoundTripsOptionalScopes()
    {
        var original = new SimsTarget(
            SimId: "sim-42",
            HouseholdId: "household-9",
            ObjectId: "object-7",
            InventoryScope: "personal");

        var roundTripped = RoundTrip(original);

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    private static T RoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private static JsonElement AssertPayloadJson(object payload)
    {
        Assert.That(payload, Is.TypeOf<JsonElement>());
        return (JsonElement)payload;
    }
}

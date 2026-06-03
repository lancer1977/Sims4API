using PolyhydraGames.Sims4.Bridge;
using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class SimsContractSurfaceTests
{
    [Test]
    public void ActionNames_IncludesInventoryAndInfluenceActions()
    {
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.RunInteraction));
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.SpawnObject));
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.AddItem));
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.TakeItem));
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.TakeStock));
        Assert.That(SimsActionNames.All, Does.Contain(SimsActionNames.TriggerInfluence));
    }

    [Test]
    public void InfluenceKinds_IncludesTheCanonicalSummons()
    {
        Assert.That(SimsInfluenceKinds.All, Does.Contain(SimsInfluenceKinds.FireIncident));
        Assert.That(SimsInfluenceKinds.All, Does.Contain(SimsInfluenceKinds.RobberBreakIn));
        Assert.That(SimsInfluenceKinds.All, Does.Contain(SimsInfluenceKinds.SurpriseGuest));
        Assert.That(SimsGuestVariants.All, Does.Contain(SimsGuestVariants.Friend));
        Assert.That(SimsGuestVariants.All, Does.Contain(SimsGuestVariants.Rival));
        Assert.That(SimsGuestVariants.All, Does.Contain(SimsGuestVariants.Neighbor));
        Assert.That(SimsGuestVariants.All, Does.Contain(SimsGuestVariants.RandomGuest));
    }

    [Test]
    public void EventNames_IncludesCapabilityAndInventorySignals()
    {
        Assert.That(SimsEventNames.All, Does.Contain(SimsEventNames.Capabilities));
        Assert.That(SimsEventNames.All, Does.Contain(SimsEventNames.InventorySnapshot));
    }

    [Test]
    public void ModCapabilities_CarriesTheBaseSurfaceFlags()
    {
        var capabilities = new SimsModCapabilities(
            ModVersion: "1.2.3",
            ApiVersion: "v1",
            SupportedActions: SimsActionNames.All,
            SupportedEvents: SimsEventNames.All,
            SupportsInventoryExposure: true,
            SupportsStocktake: true);

        Assert.That(capabilities.SupportsInventoryExposure, Is.True);
        Assert.That(capabilities.SupportsStocktake, Is.True);
        Assert.That(capabilities.SupportedActions, Does.Contain(SimsActionNames.TakeItem));
    }

    [Test]
    public void InventorySnapshot_PreservesTargetAndItems()
    {
        var target = new SimsTarget(SimId: "sim-42", HouseholdId: "household-9");
        var items = new[]
        {
            new SimsInventoryItem("item-1", "Laser Cutter", 1, false, "Debug", "Spawner"),
            new SimsInventoryItem("item-2", "Clay", 5, true, "Collectible", "Harvest")
        };

        var snapshot = new SimsInventorySnapshot(
            StreamerUserId: "streamer-1",
            CapturedAt: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            Target: target,
            Items: items,
            HouseholdFunds: 1234,
            Notes: "stocktake");

        Assert.That(snapshot.Target, Is.EqualTo(target));
        Assert.That(snapshot.Items, Is.EqualTo(items));
        Assert.That(snapshot.HouseholdFunds, Is.EqualTo(1234));
    }

    [Test]
    public void BridgeOptions_ExposeSafeDefaults()
    {
        var options = new SimsBridgeOptions();

        Assert.That(options.EventBufferPath, Is.EqualTo("event-buffer.jsonl"));
        Assert.That(options.RetryAttempts, Is.EqualTo(5));
        Assert.That(options.RetryDelayMilliseconds, Is.EqualTo(500));
    }

    [Test]
    public void CommandEnvelope_CarriesTargetAndCorrelationId()
    {
        var command = new SimsCommand(
            StreamerUserId: "streamer-1",
            Action: SimsActionNames.TakeStock,
            Timestamp: new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            Payload: new { reason = "manual" },
            Id: "cmd-1",
            Target: new SimsTarget(SimId: "sim-42"),
            CorrelationId: "corr-77");

        Assert.That(command.Action, Is.EqualTo(SimsActionNames.TakeStock));
        Assert.That(command.Target?.SimId, Is.EqualTo("sim-42"));
        Assert.That(command.CorrelationId, Is.EqualTo("corr-77"));
    }
}

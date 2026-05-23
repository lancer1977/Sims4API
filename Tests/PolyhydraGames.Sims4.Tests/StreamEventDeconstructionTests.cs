using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventDeconstructionTests
{
    [Test]
    public void StreamEvent_DeconstructsAllMembers()
    {
        var when = new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.Zero);
        var payload = new { total = 101 };
        var evt = new StreamEvent("streamer-01", "FundsChanged", when, payload, "evt-deconstruct");

        var (user, type, timestamp, payloadValue, id) = evt;

        Assert.That(user, Is.EqualTo("streamer-01"));
        Assert.That(type, Is.EqualTo("FundsChanged"));
        Assert.That(timestamp, Is.EqualTo(when));
        Assert.That(payloadValue, Is.SameAs(payload));
        Assert.That(id, Is.EqualTo("evt-deconstruct"));
    }
}

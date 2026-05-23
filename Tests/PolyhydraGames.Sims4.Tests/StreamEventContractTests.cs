using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventContractTests
{
    [Test]
    public void StreamEvent_DefaultsTimestampWhenConstructedWithUtcNow()
    {
        var evt = new StreamEvent("user-id", "Heartbeat", DateTimeOffset.UtcNow, new { status = "ok" }, "evt-001");

        Assert.That(evt.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public void StreamEvent_DuplicatesCanBeComparedByRecordValue()
    {
        var first = new StreamEvent("user-id", "Ping", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new { mode = "simple" }, "evt-002");
        var second = new StreamEvent("user-id", "Ping", first.Timestamp, new { mode = "simple" }, "evt-002");

        Assert.That(first, Is.EqualTo(second));
    }
}

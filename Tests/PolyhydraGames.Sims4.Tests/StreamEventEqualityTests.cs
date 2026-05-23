using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventEqualityTests
{
    [Test]
    public void StreamEvent_EqualRecordsMatchOnAllValues()
    {
        var when = new DateTimeOffset(2026, 2, 3, 0, 0, 0, TimeSpan.Zero);

        var first = new StreamEvent("user-x", "Notify", when, new { stage = "start" }, "evt-eq");
        var second = new StreamEvent("user-x", "Notify", when, new { stage = "start" }, "evt-eq");

        Assert.That(first, Is.EqualTo(second));
    }
}

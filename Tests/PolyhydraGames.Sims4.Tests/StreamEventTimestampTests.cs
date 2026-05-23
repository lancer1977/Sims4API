using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventTimestampTests
{
    [Test]
    public void StreamEvent_UsesProvidedTimestamp()
    {
        var fixedTime = new DateTimeOffset(2026, 2, 4, 12, 30, 0, TimeSpan.Zero);

        var evt = new StreamEvent("s", "Tick", fixedTime, new { }, "evt-time");

        Assert.That(evt.Timestamp, Is.EqualTo(fixedTime));
    }
}

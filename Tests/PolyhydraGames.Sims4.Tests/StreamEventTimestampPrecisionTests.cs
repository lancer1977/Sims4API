using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventTimestampPrecisionTests
{
    [Test]
    public void StreamEvent_PreservesTimestampOffset()
    {
        var timestamp = new DateTimeOffset(2026, 05, 22, 10, 30, 0, TimeSpan.FromHours(-5));
        var evt = new StreamEvent("streamer", "Offset", timestamp, new { }, "evt-1");

        Assert.That(evt.Timestamp, Is.EqualTo(timestamp));
    }
}

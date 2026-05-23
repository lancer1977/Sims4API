using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventPayloadEqualityTests
{
    private sealed class Payload
    {
        public int Value { get; set; }
    }

    [Test]
    public void StreamEvent_EqualityRequiresAllMembersEqual()
    {
        var timestamp = new DateTimeOffset(2026, 05, 22, 3, 0, 0, TimeSpan.Zero);
        var payload = new Payload { Value = 10 };
        var first = new StreamEvent("streamer", "Type", timestamp, payload, "id");
        var second = new StreamEvent("streamer", "Type", timestamp, payload, "id");

        Assert.That(first, Is.EqualTo(second));
    }
}

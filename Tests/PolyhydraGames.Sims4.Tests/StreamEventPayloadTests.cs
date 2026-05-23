using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventPayloadTests
{
    [Test]
    public void StreamEvent_PreservesPayloadReference()
    {
        var payload = new { message = "ready", count = 5 };

        var evt = new StreamEvent("s", "Payload", DateTimeOffset.UtcNow, payload, "evt-payload");

        Assert.That(evt.Payload, Is.SameAs(payload));
    }
}

using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventNullPayloadTests
{
    [Test]
    public void StreamEvent_AllowsNullPayload()
    {
        var evt = new StreamEvent("user-id", "Type", DateTimeOffset.UtcNow, null!, "evt-null");

        Assert.That(evt.Payload, Is.Null);
    }
}

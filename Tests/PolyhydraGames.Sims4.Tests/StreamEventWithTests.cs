using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventWithTests
{
    [Test]
    public void StreamEvent_With_OverridesIdOnly()
    {
        var original = new StreamEvent("user-id", "Type", DateTimeOffset.UtcNow, new { value = 1 }, "id-old");
        var copy = original with { Id = "id-new" };

        Assert.That(copy.Id, Is.EqualTo("id-new"));
        Assert.That(copy.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(copy.Type, Is.EqualTo(original.Type));
        Assert.That(copy.Payload, Is.SameAs(original.Payload));
        Assert.That(copy.Timestamp, Is.EqualTo(original.Timestamp));
    }
}

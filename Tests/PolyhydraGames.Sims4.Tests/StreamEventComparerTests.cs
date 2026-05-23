using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventComparerTests
{
    [Test]
    public void StreamEvent_WhenComparedToItsClone()
    {
        var evt = new StreamEvent("streamer", "Type", DateTimeOffset.UtcNow, new { value = 1 }, "id");
        var clone = new StreamEvent(evt.StreamerUserId, evt.Type, evt.Timestamp, evt.Payload, evt.Id);

        Assert.That(evt.GetHashCode(), Is.EqualTo(clone.GetHashCode()));
    }
}

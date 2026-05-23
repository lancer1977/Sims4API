using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventCreationTests
{
    [Test]
    public void StreamEvent_CreatesWithProvidedFields()
    {
        var when = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var item = new StreamEvent("stream-user", "CommandQueued", when, new { value = 11 }, "evt-creation");

        Assert.That(item.StreamerUserId, Is.EqualTo("stream-user"));
        Assert.That(item.Type, Is.EqualTo("CommandQueued"));
        Assert.That(item.Timestamp, Is.EqualTo(when));
        Assert.That(item.Id, Is.EqualTo("evt-creation"));
    }
}

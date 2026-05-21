using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventTests
{
    [Test]
    public void StreamEvent_CanBeCreated()
    {
        var evt = new StreamEvent(
            StreamerUserId: "test_user",
            Type: "TestType",
            Timestamp: DateTimeOffset.UtcNow,
            Payload: new { foo = "bar" },
            Id: Guid.NewGuid().ToString()
        );

        Assert.That(evt.StreamerUserId, Is.EqualTo("test_user"));
        Assert.That(evt.Type, Is.EqualTo("TestType"));
    }
}

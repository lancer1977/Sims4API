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

    [Test]
    public void StreamEvent_ExposesTimestamp_WhenProvided()
    {
        var when = new DateTimeOffset(2026, 01, 01, 12, 34, 56, TimeSpan.Zero);

        var evt = new StreamEvent("user-id", "Type", when, new { value = 42 }, "evt-01");

        Assert.That(evt.Timestamp, Is.EqualTo(when));
    }

    [Test]
    public void StreamEvent_RetainsPayloadObject()
    {
        var payload = new { foo = "bar", count = 3 };

        var evt = new StreamEvent("user-id", "Type", DateTimeOffset.UtcNow, payload, "evt-02");

        Assert.That(evt.Payload, Is.SameAs(payload));
    }

    [Test]
    public void StreamEvent_IsValueBased_Equality()
    {
        var first = new StreamEvent("user-id", "Type", DateTimeOffset.UtcNow, new { foo = "bar" }, "evt-03");
        var second = new StreamEvent("user-id", "Type", first.Timestamp, first.Payload, "evt-03");

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void StreamEvent_Deconstruct_MapsAllPositionalMembers()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var expectedPayload = new { value = 13 };
        var evt = new StreamEvent("streamer", "Type", timestamp, expectedPayload, "evt-04");

        var (streamerUserId, type, actualTimestamp, payload, id) = evt;

        Assert.That(streamerUserId, Is.EqualTo("streamer"));
        Assert.That(type, Is.EqualTo("Type"));
        Assert.That(actualTimestamp, Is.EqualTo(timestamp));
        Assert.That(payload, Is.SameAs(expectedPayload));
        Assert.That(id, Is.EqualTo("evt-04"));
    }

    [Test]
    public void StreamEvent_WithExpression_ClonesValueAndOverridesSelectedMember()
    {
        var original = new StreamEvent("streamer", "Type", DateTimeOffset.UtcNow, new { value = 13 }, "evt-05");

        var copy = original with { Type = "UpdatedType", Id = "evt-05-copy" };

        Assert.That(copy.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(copy.Timestamp, Is.EqualTo(original.Timestamp));
        Assert.That(copy.Payload, Is.SameAs(original.Payload));
        Assert.That(copy.Type, Is.EqualTo("UpdatedType"));
        Assert.That(copy.Id, Is.EqualTo("evt-05-copy"));
    }
}

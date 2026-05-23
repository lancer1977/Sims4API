using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventToStringTests
{
    [Test]
    public void StreamEvent_ToString_ContainsTypeAndUserId()
    {
        var evt = new StreamEvent("streamer", "Update", new DateTimeOffset(2026, 05, 22, 1, 2, 3, TimeSpan.Zero), new { value = 1 }, "evt-99");
        var rendered = evt.ToString();

        Assert.That(rendered, Does.Contain("Update"));
        Assert.That(rendered, Does.Contain("streamer"));
        Assert.That(rendered, Does.Contain("evt-99"));
    }
}

using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventIdOverrideTests
{
    [Test]
    public void StreamEvent_WithChangesIdAndTypeTogether()
    {
        var original = new StreamEvent("streamer", "Type", DateTimeOffset.UtcNow, new { status = "orig" }, "original-id");
        var copied = original with { Id = "copied-id", Type = "Type2" };

        Assert.That(copied.StreamerUserId, Is.EqualTo(original.StreamerUserId));
        Assert.That(copied.Type, Is.EqualTo("Type2"));
        Assert.That(copied.Id, Is.EqualTo("copied-id"));
    }
}

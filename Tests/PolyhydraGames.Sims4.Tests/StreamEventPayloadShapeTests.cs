using PolyhydraGames.Sims4.Core;
using NUnit.Framework;

namespace PolyhydraGames.Sims4.Tests;

public class StreamEventPayloadShapeTests
{
    [Test]
    public void StreamEvent_WithDifferentPayloadTypesRetainsReferenceType()
    {
        var dictPayload = new { value = 1 };
        var evt = new StreamEvent("s", "Type", DateTimeOffset.UtcNow, dictPayload, "id");

        Assert.That(evt.Payload, Is.SameAs(dictPayload));
    }
}

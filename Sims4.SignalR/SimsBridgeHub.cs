using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Bridge;

public sealed class SimsBridgeHub(SimsBridgeHostState state, ILogger<SimsBridgeHub> logger) : Hub
{
    public Task SubmitEvent(StreamEvent evt)
    {
        state.MarkEvent(evt);
        logger.LogInformation("Captured Sims event {Type} with id {Id}.", evt.Type, evt.Id);
        return Task.CompletedTask;
    }
}

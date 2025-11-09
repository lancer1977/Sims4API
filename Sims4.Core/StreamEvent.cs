namespace PolyhydraGames.Sims4.Core;
public sealed record StreamEvent(
    string StreamerUserId,
    string Type,                 // e.g., "CommandQueued", "CommandProcessed", "FundsChanged"
    DateTimeOffset Timestamp,
    object Payload,              // anonymous/JSON payload
    string Id                    // idempotency
);

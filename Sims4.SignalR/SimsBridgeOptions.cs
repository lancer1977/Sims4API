namespace PolyhydraGames.Sims4.Bridge;

public sealed class SimsBridgeOptions
{
    public string HubUrl { get; set; } = string.Empty;

    public string WebKey { get; set; } = string.Empty;

    public string EventBufferPath { get; set; } = "event-buffer.jsonl";

    public int RetryAttempts { get; set; } = 5;

    public int RetryDelayMilliseconds { get; set; } = 500;
}

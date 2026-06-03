using System.Text.Json;
using PolyhydraGames.Sims4.Core;

namespace PolyhydraGames.Sims4.Bridge;

internal static class SimsBridgeBuffer
{
    internal static async Task AppendAsync(StreamEvent evt, string bufferPath, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(bufferPath)
            ? "event-buffer.jsonl"
            : bufferPath;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(evt) + Environment.NewLine;
        await File.AppendAllTextAsync(path, line, cancellationToken);
    }
}

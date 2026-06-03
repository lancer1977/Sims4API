using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyhydraGames.Sims4.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SimsCommandDispatchStatus
{
    Unhandled,
    Pending,
    Processing,
    Processed,
    Failed,
}

public sealed record SimsCommandHandlerResult(
    SimsCommandDispatchStatus Status,
    string? Message = null,
    string? FailureCode = null,
    string? FailureReason = null,
    string? FailureStack = null,
    string? RawErrorPayload = null)
{
    public static SimsCommandHandlerResult Processed(string? message = null) =>
        new(SimsCommandDispatchStatus.Processed, message);

    public static SimsCommandHandlerResult Failed(
        string message,
        string? failureCode = null,
        string? rawErrorPayload = null,
        string? failureStack = null) =>
        new(
            SimsCommandDispatchStatus.Failed,
            message,
            failureCode,
            message,
            failureStack,
            rawErrorPayload);
}

public interface ISimsCommandHandler
{
    bool CanHandle(SimsCommand command);

    Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default);
}

public sealed record SimsCommandDispatchRecord(
    string CommandId,
    string StreamerUserId,
    string Action,
    DateTimeOffset CommandTimestamp,
    DateTimeOffset DispatchedAt,
    SimsCommandDispatchStatus Status,
    string? HandlerName = null,
    string? Message = null,
    SimsTarget? Target = null,
    string? CorrelationId = null,
    DateTimeOffset? ProcessingAt = null,
    DateTimeOffset? ProcessedAt = null,
    DateTimeOffset? FailedAt = null,
    string? FailureReason = null,
    string? FailureCode = null,
    string? FailureStack = null,
    string? RawErrorPayload = null);

public sealed class SimsCommandDispatcher
{
    private readonly IReadOnlyList<ISimsCommandHandler> _handlers;
    private readonly SimsCommandDispatchJournal _journal;

    public SimsCommandDispatcher(IEnumerable<ISimsCommandHandler> handlers, string journalPath = "command-status.jsonl")
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _handlers = handlers.ToArray();
        _journal = new SimsCommandDispatchJournal(journalPath);
    }

    public async Task<SimsCommandDispatchRecord> DispatchAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var current = CreateRecord(command, SimsCommandDispatchStatus.Pending, dispatchedAt: startedAt);
        current = await _journal.AppendAsync(current, cancellationToken);

        var handler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(command));
        if (handler is null)
        {
            return await _journal.TransitionAsync(
                current,
                SimsCommandDispatchStatus.Unhandled,
                handlerName: null,
                message: $"No handler registered for action '{command.Action}'.",
                failureCode: "NO_HANDLER",
                failureReason: $"No handler registered for action '{command.Action}'.",
                rawErrorPayload: SerializePayloadForAudit(command.Payload),
                cancellationToken: cancellationToken);
        }

        current = await _journal.TransitionAsync(
            current,
            SimsCommandDispatchStatus.Processing,
            handlerName: handler.GetType().Name,
            message: "Processing command.",
            cancellationToken: cancellationToken);

        try
        {
            var result = await handler.HandleAsync(command, cancellationToken);
            var finalStatus = result.Status == SimsCommandDispatchStatus.Processed
                ? SimsCommandDispatchStatus.Processed
                : result.Status;

            if (finalStatus == SimsCommandDispatchStatus.Failed)
            {
                return await _journal.TransitionAsync(
                    current,
                    SimsCommandDispatchStatus.Failed,
                    handlerName: handler.GetType().Name,
                    message: result.Message,
                    failureCode: result.FailureCode,
                    failureReason: result.FailureReason,
                    failureStack: result.FailureStack,
                    rawErrorPayload: result.RawErrorPayload,
                    cancellationToken: cancellationToken);
            }

            return await _journal.TransitionAsync(
                current,
                SimsCommandDispatchStatus.Processed,
                handlerName: handler.GetType().Name,
                message: result.Message,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return await _journal.TransitionAsync(
                current,
                SimsCommandDispatchStatus.Failed,
                handlerName: handler.GetType().Name,
                message: ex.Message,
                failureCode: "HANDLER_EXCEPTION",
                failureReason: ex.Message,
                failureStack: ex.ToString(),
                rawErrorPayload: SerializePayloadForAudit(command.Payload),
                cancellationToken: cancellationToken);
        }
    }

    public Task<SimsCommandDispatchRecord?> ReadLatestAsync(string commandId, CancellationToken cancellationToken = default) =>
        _journal.ReadLatestAsync(commandId, cancellationToken);

    private static SimsCommandDispatchRecord CreateRecord(
        SimsCommand command,
        SimsCommandDispatchStatus status,
        DateTimeOffset? dispatchedAt = null,
        string? handlerName = null,
        string? message = null,
        DateTimeOffset? processingAt = null,
        DateTimeOffset? processedAt = null,
        DateTimeOffset? failedAt = null,
        string? failureReason = null,
        string? failureCode = null,
        string? failureStack = null,
        string? rawErrorPayload = null) =>
        new(
            command.Id,
            command.StreamerUserId,
            command.Action,
            command.Timestamp,
            dispatchedAt ?? DateTimeOffset.UtcNow,
            status,
            handlerName,
            message,
            command.Target,
            command.CorrelationId,
            processingAt,
            processedAt,
            failedAt,
            failureReason,
            failureCode,
            failureStack,
            rawErrorPayload);

    private static string? SerializePayloadForAudit(object? payload)
    {
        return payload is null ? null : JsonSerializer.Serialize(payload);
    }
}

public sealed class SimsCommandDispatchJournal
{
    private static readonly SemaphoreSlim _journalWriteLock = new(1, 1);
    private readonly string _journalPath;

    public SimsCommandDispatchJournal(string journalPath = "command-status.jsonl")
    {
        _journalPath = string.IsNullOrWhiteSpace(journalPath) ? "command-status.jsonl" : journalPath;
    }

    public async Task<SimsCommandDispatchRecord> AppendAsync(SimsCommandDispatchRecord record, CancellationToken cancellationToken = default)
    {
        await _journalWriteLock.WaitAsync(cancellationToken);
        try
        {
            await WriteLineAsync(record, cancellationToken);
            return record;
        }
        finally
        {
            _journalWriteLock.Release();
        }
    }

    public async Task<SimsCommandDispatchRecord> TransitionAsync(
        SimsCommandDispatchRecord current,
        SimsCommandDispatchStatus status,
        string? handlerName = null,
        string? message = null,
        string? failureCode = null,
        string? failureReason = null,
        string? failureStack = null,
        string? rawErrorPayload = null,
        CancellationToken cancellationToken = default)
    {
        var next = BuildTransitionRecord(
            current,
            status,
            handlerName,
            message,
            failureCode,
            failureReason,
            failureStack,
            rawErrorPayload);

        return await AppendAsync(next, cancellationToken);
    }

    public async Task<SimsCommandDispatchRecord?> TryTransitionAsync(
        string commandId,
        SimsCommandDispatchStatus expectedCurrentStatus,
        SimsCommandDispatchStatus status,
        string? handlerName = null,
        string? message = null,
        string? failureCode = null,
        string? failureReason = null,
        string? failureStack = null,
        string? rawErrorPayload = null,
        CancellationToken cancellationToken = default)
    {
        await _journalWriteLock.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadAllLockedAsync(cancellationToken);
            var current = records.LastOrDefault(record => record.CommandId == commandId);
            if (current is null || current.Status != expectedCurrentStatus)
            {
                return null;
            }

            var next = BuildTransitionRecord(
                current,
                status,
                handlerName,
                message,
                failureCode,
                failureReason,
                failureStack,
                rawErrorPayload);

            await WriteLineAsync(next, cancellationToken);
            return next;
        }
        finally
        {
            _journalWriteLock.Release();
        }
    }

    public async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadHistoryAsync(
        SimsCommandDispatchStatus status,
        CancellationToken cancellationToken = default)
    {
        var records = await ReadAllAsync(cancellationToken);
        return records.Where(record => record.Status == status).ToList();
    }

    public async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadProcessedHistoryAsync(CancellationToken cancellationToken = default) =>
        await ReadHistoryAsync(SimsCommandDispatchStatus.Processed, cancellationToken);

    public async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadFailedHistoryAsync(CancellationToken cancellationToken = default) =>
        await ReadHistoryAsync(SimsCommandDispatchStatus.Failed, cancellationToken);

    public async Task<SimsCommandDispatchRecord?> ReadLatestAsync(string commandId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            throw new ArgumentException("Command id cannot be blank.", nameof(commandId));
        }

        var records = await ReadAllAsync(cancellationToken);
        return records.LastOrDefault(record => record.CommandId == commandId);
    }

    public async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        await _journalWriteLock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllLockedAsync(cancellationToken);
        }
        finally
        {
            _journalWriteLock.Release();
        }
    }

    private static SimsCommandDispatchRecord BuildTransitionRecord(
        SimsCommandDispatchRecord current,
        SimsCommandDispatchStatus status,
        string? handlerName,
        string? message,
        string? failureCode,
        string? failureReason,
        string? failureStack,
        string? rawErrorPayload)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var next = current with
        {
            Status = status,
            HandlerName = handlerName ?? current.HandlerName,
            Message = message,
            ProcessingAt = status == SimsCommandDispatchStatus.Processing
                ? timestamp
                : current.ProcessingAt,
            ProcessedAt = status == SimsCommandDispatchStatus.Processed
                ? timestamp
                : current.ProcessedAt,
            FailedAt = status == SimsCommandDispatchStatus.Failed || status == SimsCommandDispatchStatus.Unhandled
                ? timestamp
                : current.FailedAt,
            FailureCode = status == SimsCommandDispatchStatus.Failed || status == SimsCommandDispatchStatus.Unhandled
                ? failureCode
                : current.FailureCode,
            FailureReason = status == SimsCommandDispatchStatus.Failed || status == SimsCommandDispatchStatus.Unhandled
                ? failureReason
                : current.FailureReason,
            FailureStack = status == SimsCommandDispatchStatus.Failed || status == SimsCommandDispatchStatus.Unhandled
                ? failureStack
                : current.FailureStack,
            RawErrorPayload = status == SimsCommandDispatchStatus.Failed || status == SimsCommandDispatchStatus.Unhandled
                ? rawErrorPayload
                : current.RawErrorPayload,
        };

        if (status is not SimsCommandDispatchStatus.Failed and not SimsCommandDispatchStatus.Unhandled)
        {
            next = next with
            {
                FailureCode = current.FailureCode,
                FailureReason = current.FailureReason,
                FailureStack = current.FailureStack,
                RawErrorPayload = current.RawErrorPayload,
            };
        }

        return next;
    }

    private async Task WriteLineAsync(SimsCommandDispatchRecord record, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_journalPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(record) + Environment.NewLine;
        await File.AppendAllTextAsync(_journalPath, line, cancellationToken);
    }

    private async Task<IReadOnlyList<SimsCommandDispatchRecord>> ReadAllLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_journalPath))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(_journalPath, cancellationToken);
        var result = new List<SimsCommandDispatchRecord>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var record = JsonSerializer.Deserialize<SimsCommandDispatchRecord>(line);
            if (record is null)
            {
                continue;
            }

            result.Add(record);
        }

        return result;
    }
}

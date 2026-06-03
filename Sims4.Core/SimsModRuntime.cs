using System.Text.Json;

namespace PolyhydraGames.Sims4.Core;

public sealed record SimsAddItemRequest(
    string ItemId,
    string DisplayName,
    int Quantity = 1,
    bool IsStackable = true,
    string? Category = null,
    string? Source = null);

public sealed record SimsTakeItemRequest(
    string ItemId,
    int Quantity = 1,
    string? Notes = null);

public sealed record SimsTakeStockRequest(string? Notes = null);

public sealed record SimsRunInteractionRequest(
    DateTimeOffset RequestedAt,
    string InteractionId,
    string? InteractionName = null,
    SimsTarget? Target = null,
    string? QueueContext = null,
    bool RequiresApproval = false,
    string? ApprovalToken = null);

public sealed record SimsSpawnObjectRequest(
    DateTimeOffset RequestedAt,
    string ObjectId,
    string DisplayName,
    SimsTarget? Target = null,
    int Quantity = 1,
    string? Placement = null,
    string? Notes = null,
    bool RequiresApproval = false,
    string? ApprovalToken = null);

public static class SimsCommandPayloadExtensions
{
    public static T? GetPayload<T>(this SimsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var json = JsonSerializer.Serialize(command.Payload);
        return JsonSerializer.Deserialize<T>(json);
    }
}

public interface ISimsInventoryStore
{
    SimsCommandHandlerResult AddItem(string streamerUserId, SimsTarget? target, SimsAddItemRequest request);

    SimsCommandHandlerResult TakeItem(string streamerUserId, SimsTarget? target, SimsTakeItemRequest request);

    SimsInventorySnapshot CaptureSnapshot(string streamerUserId, SimsTarget? target, string? notes = null);
}

public sealed class SimsInventoryStore : ISimsInventoryStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, SimsInventoryItem>> _inventories = new(StringComparer.OrdinalIgnoreCase);

    public SimsInventorySnapshot? LastSnapshot { get; private set; }

    public SimsCommandHandlerResult AddItem(string streamerUserId, SimsTarget? target, SimsAddItemRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return SimsCommandHandlerResult.Failed(validation);
        }

        lock (_gate)
        {
            var items = GetInventory(target);
            if (items.TryGetValue(request.ItemId, out var existing))
            {
                items[request.ItemId] = existing with
                {
                    DisplayName = request.DisplayName,
                    Quantity = existing.Quantity + request.Quantity,
                    IsStackable = request.IsStackable,
                    Category = request.Category ?? existing.Category,
                    Source = request.Source ?? existing.Source,
                };
            }
            else
            {
                items[request.ItemId] = new SimsInventoryItem(
                    request.ItemId,
                    request.DisplayName,
                    request.Quantity,
                    request.IsStackable,
                    request.Category,
                    request.Source);
            }

            return SimsCommandHandlerResult.Processed($"Added {request.Quantity} x {request.DisplayName}.");
        }
    }

    public SimsCommandHandlerResult TakeItem(string streamerUserId, SimsTarget? target, SimsTakeItemRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return SimsCommandHandlerResult.Failed(validation);
        }

        lock (_gate)
        {
            var items = GetInventory(target);
            if (!items.TryGetValue(request.ItemId, out var existing))
            {
                return SimsCommandHandlerResult.Failed($"Item '{request.ItemId}' is not available.");
            }

            if (existing.Quantity < request.Quantity)
            {
                return SimsCommandHandlerResult.Failed($"Item '{existing.DisplayName}' only has {existing.Quantity} available.");
            }

            var newQuantity = existing.Quantity - request.Quantity;
            if (newQuantity > 0)
            {
                items[request.ItemId] = existing with { Quantity = newQuantity };
            }
            else
            {
                items.Remove(request.ItemId);
            }

            return SimsCommandHandlerResult.Processed($"Removed {request.Quantity} x {existing.DisplayName}.");
        }
    }

    public SimsInventorySnapshot CaptureSnapshot(string streamerUserId, SimsTarget? target, string? notes = null)
    {
        lock (_gate)
        {
            var items = GetInventory(target)
                .Values
                .Select(item => item with { })
                .ToArray();

            var snapshot = new SimsInventorySnapshot(
                streamerUserId,
                DateTimeOffset.UtcNow,
                target,
                items,
                null,
                notes);

            LastSnapshot = snapshot;
            return snapshot;
        }
    }

    private Dictionary<string, SimsInventoryItem> GetInventory(SimsTarget? target)
    {
        var key = GetTargetKey(target);
        if (!_inventories.TryGetValue(key, out var items))
        {
            items = new Dictionary<string, SimsInventoryItem>(StringComparer.OrdinalIgnoreCase);
            _inventories[key] = items;
        }

        return items;
    }

    private static string GetTargetKey(SimsTarget? target)
    {
        if (!string.IsNullOrWhiteSpace(target?.InventoryScope))
        {
            return $"scope:{target.InventoryScope}";
        }

        if (!string.IsNullOrWhiteSpace(target?.HouseholdId))
        {
            return $"household:{target.HouseholdId}";
        }

        if (!string.IsNullOrWhiteSpace(target?.SimId))
        {
            return $"sim:{target.SimId}";
        }

        if (!string.IsNullOrWhiteSpace(target?.ObjectId))
        {
            return $"object:{target.ObjectId}";
        }

        return "default";
    }

    private static string? ValidateRequest(SimsAddItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            return "ItemId is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "DisplayName is required.";
        }

        return null;
    }

    private static string? ValidateRequest(SimsTakeItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            return "ItemId is required.";
        }

        return null;
    }
}

public sealed class SimsAddItemHandler(ISimsInventoryStore store) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.AddItem;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.GetPayload<SimsAddItemRequest>();
        if (request is null)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed("add_item payload was missing or malformed."));
        }

        return Task.FromResult(store.AddItem(command.StreamerUserId, command.Target, request));
    }
}

public sealed class SimsTakeItemHandler(ISimsInventoryStore store) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.TakeItem;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.GetPayload<SimsTakeItemRequest>();
        if (request is null)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed("take_item payload was missing or malformed."));
        }

        return Task.FromResult(store.TakeItem(command.StreamerUserId, command.Target, request));
    }
}

public sealed class SimsTakeStockHandler(ISimsInventoryStore store) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.TakeStock;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.GetPayload<SimsTakeStockRequest>() ?? new SimsTakeStockRequest();
        var snapshot = store.CaptureSnapshot(command.StreamerUserId, command.Target, request.Notes);
        return Task.FromResult(SimsCommandHandlerResult.Processed($"Captured {snapshot.Items.Count} inventory items."));
    }
}

public sealed record SimsWorldActionRecord(
    string StreamerUserId,
    string Action,
    string PrimaryId,
    string DisplayName,
    DateTimeOffset RequestedAt,
    SimsTarget? Target,
    int Quantity = 1,
    string? Placement = null,
    string? Notes = null);

public interface ISimsWorldActionStore
{
    IReadOnlyList<SimsWorldActionRecord> Actions { get; }

    SimsWorldActionRecord? LastInteraction { get; }

    SimsWorldActionRecord? LastSpawnedObject { get; }

    SimsCommandHandlerResult RunInteraction(string streamerUserId, SimsTarget? target, SimsRunInteractionRequest request);

    SimsCommandHandlerResult SpawnObject(string streamerUserId, SimsTarget? target, SimsSpawnObjectRequest request);
}

public sealed class SimsWorldActionStore : ISimsWorldActionStore
{
    private readonly object _gate = new();
    private readonly List<SimsWorldActionRecord> _actions = [];

    public IReadOnlyList<SimsWorldActionRecord> Actions
    {
        get
        {
            lock (_gate)
            {
                return _actions.ToArray();
            }
        }
    }

    public SimsWorldActionRecord? LastInteraction { get; private set; }

    public SimsWorldActionRecord? LastSpawnedObject { get; private set; }

    public SimsCommandHandlerResult RunInteraction(string streamerUserId, SimsTarget? target, SimsRunInteractionRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return SimsCommandHandlerResult.Failed(validation);
        }

        lock (_gate)
        {
            var displayName = string.IsNullOrWhiteSpace(request.InteractionName)
                ? request.InteractionId
                : request.InteractionName;
            var record = new SimsWorldActionRecord(
                StreamerUserId: streamerUserId,
                Action: SimsActionNames.RunInteraction,
                PrimaryId: request.InteractionId,
                DisplayName: displayName,
                RequestedAt: request.RequestedAt,
                Target: target ?? request.Target,
                Notes: request.QueueContext);

            _actions.Add(record);
            LastInteraction = record;

            var scope = DescribeTarget(target ?? request.Target);
            var context = string.IsNullOrWhiteSpace(request.QueueContext) ? string.Empty : $" Context: {request.QueueContext}.";
            return SimsCommandHandlerResult.Processed($"Queued interaction '{displayName}' ({request.InteractionId}) for {scope}.{context}".Trim());
        }
    }

    public SimsCommandHandlerResult SpawnObject(string streamerUserId, SimsTarget? target, SimsSpawnObjectRequest request)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return SimsCommandHandlerResult.Failed(validation);
        }

        lock (_gate)
        {
            var record = new SimsWorldActionRecord(
                StreamerUserId: streamerUserId,
                Action: SimsActionNames.SpawnObject,
                PrimaryId: request.ObjectId,
                DisplayName: request.DisplayName,
                RequestedAt: request.RequestedAt,
                Target: target ?? request.Target,
                Quantity: request.Quantity,
                Placement: request.Placement,
                Notes: request.Notes);

            _actions.Add(record);
            LastSpawnedObject = record;

            var scope = DescribeTarget(target ?? request.Target);
            var placement = string.IsNullOrWhiteSpace(request.Placement) ? "the lot" : request.Placement;
            return SimsCommandHandlerResult.Processed($"Spawned {request.Quantity} x {request.DisplayName} ({request.ObjectId}) at {placement} for {scope}.");
        }
    }

    private static string? ValidateRequest(SimsRunInteractionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InteractionId))
        {
            return "InteractionId is required.";
        }

        return null;
    }

    private static string? ValidateRequest(SimsSpawnObjectRequest request)
    {
        if (request.Quantity <= 0)
        {
            return "Quantity must be greater than zero.";
        }

        if (string.IsNullOrWhiteSpace(request.ObjectId))
        {
            return "ObjectId is required.";
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "DisplayName is required.";
        }

        return null;
    }

    private static string DescribeTarget(SimsTarget? target)
    {
        if (!string.IsNullOrWhiteSpace(target?.SimId))
        {
            return $"sim {target.SimId}";
        }

        if (!string.IsNullOrWhiteSpace(target?.HouseholdId))
        {
            return $"household {target.HouseholdId}";
        }

        if (!string.IsNullOrWhiteSpace(target?.ObjectId))
        {
            return $"object {target.ObjectId}";
        }

        return "the lot";
    }
}

public sealed class SimsRunInteractionHandler(ISimsWorldActionStore store) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.RunInteraction;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.GetPayload<SimsRunInteractionRequest>();
        if (request is null)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed("run_interaction payload was missing or malformed."));
        }

        return Task.FromResult(store.RunInteraction(command.StreamerUserId, command.Target, request));
    }
}

public sealed class SimsSpawnObjectHandler(ISimsWorldActionStore store) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.SpawnObject;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.GetPayload<SimsSpawnObjectRequest>();
        if (request is null)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed("spawn_object payload was missing or malformed."));
        }

        return Task.FromResult(store.SpawnObject(command.StreamerUserId, command.Target, request));
    }
}

public sealed record SimsInfluenceDecision(
    string StreamerUserId,
    string Kind,
    DateTimeOffset RequestedAt,
    DateTimeOffset EvaluatedAt,
    bool Allowed,
    string Message,
    string? RejectionReason = null,
    DateTimeOffset? CooldownUntil = null,
    SimsTarget? Target = null,
    string? Reason = null);

public interface ISimsInfluenceGate
{
    IReadOnlyList<SimsInfluenceDecision> AuditTrail { get; }

    SimsInfluenceDecision Evaluate(string streamerUserId, SimsInfluenceRequest request);
}

public sealed class SimsInfluenceGate : ISimsInfluenceGate
{
    private readonly object _gate = new();
    private readonly HashSet<string> _allowedKinds;
    private readonly HashSet<string> _approvalRequiredKinds;
    private readonly TimeSpan _cooldown;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly List<SimsInfluenceDecision> _auditTrail = [];
    private readonly Dictionary<string, DateTimeOffset> _lastTriggeredAt = new(StringComparer.OrdinalIgnoreCase);

    public SimsInfluenceGate(
        IEnumerable<string>? allowedKinds = null,
        IEnumerable<string>? approvalRequiredKinds = null,
        TimeSpan? cooldown = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _allowedKinds = new HashSet<string>(allowedKinds ?? SimsInfluenceKinds.All, StringComparer.OrdinalIgnoreCase);
        _approvalRequiredKinds = new HashSet<string>(approvalRequiredKinds ?? [SimsInfluenceKinds.FireIncident, SimsInfluenceKinds.RobberBreakIn], StringComparer.OrdinalIgnoreCase);
        _cooldown = cooldown ?? TimeSpan.FromMinutes(10);
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public IReadOnlyList<SimsInfluenceDecision> AuditTrail
    {
        get
        {
            lock (_gate)
            {
                return _auditTrail.ToArray();
            }
        }
    }

    public SimsInfluenceDecision Evaluate(string streamerUserId, SimsInfluenceRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamerUserId);
        ArgumentNullException.ThrowIfNull(request);

        var evaluatedAt = _utcNow();
        SimsInfluenceDecision decision;

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(request.Kind))
            {
                decision = Reject(streamerUserId, request, evaluatedAt, "Influence kind is required.");
            }
            else if (!_allowedKinds.Contains(request.Kind))
            {
                decision = Reject(streamerUserId, request, evaluatedAt, $"Influence kind '{request.Kind}' is not enabled.");
            }
            else if ((request.RequiresApproval || _approvalRequiredKinds.Contains(request.Kind)) && string.IsNullOrWhiteSpace(request.ApprovalToken))
            {
                decision = Reject(streamerUserId, request, evaluatedAt, $"Influence kind '{request.Kind}' requires approval before it can fire.");
            }
            else if (_lastTriggeredAt.TryGetValue(request.Kind, out var lastTriggeredAt))
            {
                var cooldownUntil = lastTriggeredAt + _cooldown;
                if (evaluatedAt < cooldownUntil)
                {
                    decision = Reject(streamerUserId, request, evaluatedAt, $"Influence kind '{request.Kind}' is cooling down until {cooldownUntil:O}.", cooldownUntil);
                }
                else
                {
                    decision = Approve(streamerUserId, request, evaluatedAt);
                    _lastTriggeredAt[request.Kind] = evaluatedAt;
                }
            }
            else
            {
                decision = Approve(streamerUserId, request, evaluatedAt);
                _lastTriggeredAt[request.Kind] = evaluatedAt;
            }

            _auditTrail.Add(decision);
        }

        return decision;
    }

    private static SimsInfluenceDecision Approve(string streamerUserId, SimsInfluenceRequest request, DateTimeOffset evaluatedAt)
    {
        var message = request.Reason is null
            ? $"Approved influence '{request.Kind}' for {streamerUserId}."
            : $"Approved influence '{request.Kind}' for {streamerUserId}: {request.Reason}";

        return new SimsInfluenceDecision(
            streamerUserId,
            request.Kind,
            request.RequestedAt,
            evaluatedAt,
            Allowed: true,
            Message: message,
            Target: request.Target,
            Reason: request.Reason);
    }

    private static SimsInfluenceDecision Reject(string streamerUserId, SimsInfluenceRequest request, DateTimeOffset evaluatedAt, string rejectionReason, DateTimeOffset? cooldownUntil = null)
        => new(
            streamerUserId,
            request.Kind,
            request.RequestedAt,
            evaluatedAt,
            Allowed: false,
            Message: rejectionReason,
            RejectionReason: rejectionReason,
            CooldownUntil: cooldownUntil,
            Target: request.Target,
            Reason: request.Reason);
}

public sealed class SimsTriggerInfluenceHandler(ISimsInfluenceGate gate) : ISimsCommandHandler
{
    public bool CanHandle(SimsCommand command) => command.Action == SimsActionNames.TriggerInfluence;

    public Task<SimsCommandHandlerResult> HandleAsync(SimsCommand command, CancellationToken cancellationToken = default)
    {
        var baseRequest = command.GetPayload<SimsInfluenceRequest>();
        if (baseRequest is null)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed("trigger_influence payload was missing or malformed."));
        }

        if (string.Equals(baseRequest.Kind, SimsInfluenceKinds.FireIncident, StringComparison.OrdinalIgnoreCase))
        {
            var fireRequest = command.GetPayload<SimsFireIncidentRequest>() ?? new SimsFireIncidentRequest(
                RequestedAt: baseRequest.RequestedAt,
                Reason: baseRequest.Reason,
                Target: baseRequest.Target,
                RequiresApproval: baseRequest.RequiresApproval,
                ApprovalToken: baseRequest.ApprovalToken);

            return HandleFireIncidentAsync(command.StreamerUserId, fireRequest);
        }

        if (string.Equals(baseRequest.Kind, SimsInfluenceKinds.RobberBreakIn, StringComparison.OrdinalIgnoreCase))
        {
            var robberRequest = command.GetPayload<SimsRobberBreakInRequest>() ?? new SimsRobberBreakInRequest(
                RequestedAt: baseRequest.RequestedAt,
                Reason: baseRequest.Reason,
                Target: baseRequest.Target,
                RequiresApproval: baseRequest.RequiresApproval,
                ApprovalToken: baseRequest.ApprovalToken);

            return HandleRobberBreakInAsync(command.StreamerUserId, robberRequest);
        }

        if (string.Equals(baseRequest.Kind, SimsInfluenceKinds.SurpriseGuest, StringComparison.OrdinalIgnoreCase))
        {
            var guestRequest = command.GetPayload<SimsSurpriseGuestRequest>() ?? new SimsSurpriseGuestRequest(
                RequestedAt: baseRequest.RequestedAt,
                Reason: baseRequest.Reason,
                Target: baseRequest.Target,
                GuestVariant: SimsGuestVariants.RandomGuest,
                GuestName: null,
                Venue: null,
                AnnounceArrival: true,
                RequiresApproval: baseRequest.RequiresApproval,
                ApprovalToken: baseRequest.ApprovalToken);

            return HandleSurpriseGuestAsync(command.StreamerUserId, guestRequest);
        }

        var decision = gate.Evaluate(command.StreamerUserId, baseRequest);
        if (!decision.Allowed)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed(decision.RejectionReason ?? decision.Message));
        }

        return Task.FromResult(SimsCommandHandlerResult.Processed(decision.Message));
    }

    private Task<SimsCommandHandlerResult> HandleFireIncidentAsync(string streamerUserId, SimsFireIncidentRequest request)
    {
        var decision = gate.Evaluate(streamerUserId, request);
        if (!decision.Allowed)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed(decision.RejectionReason ?? decision.Message));
        }

        var room = string.IsNullOrWhiteSpace(request.Room) ? "the lot" : request.Room;
        var severity = string.IsNullOrWhiteSpace(request.Severity) ? "medium" : request.Severity;
        var mitigation = request.NotifyFirefighters ? "Firefighters notified." : "Firefighters not notified.";
        var message = $"Fire incident staged for {room} at {severity} severity. {mitigation}";

        return Task.FromResult(SimsCommandHandlerResult.Processed(message));
    }

    private Task<SimsCommandHandlerResult> HandleRobberBreakInAsync(string streamerUserId, SimsRobberBreakInRequest request)
    {
        var decision = gate.Evaluate(streamerUserId, request);
        if (!decision.Allowed)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed(decision.RejectionReason ?? decision.Message));
        }

        var entryPoint = string.IsNullOrWhiteSpace(request.EntryPoint) ? "the front door" : request.EntryPoint;
        var severity = string.IsNullOrWhiteSpace(request.Severity) ? "medium" : request.Severity;
        var style = request.Stealthy ? "stealthy" : "loud";
        var notification = request.PoliceNotified ? "Police notified." : "Police not notified.";
        var message = $"Robber break-in staged via {entryPoint} at {severity} severity in {style} mode. {notification}";

        return Task.FromResult(SimsCommandHandlerResult.Processed(message));
    }

    private Task<SimsCommandHandlerResult> HandleSurpriseGuestAsync(string streamerUserId, SimsSurpriseGuestRequest request)
    {
        var decision = gate.Evaluate(streamerUserId, request);
        if (!decision.Allowed)
        {
            return Task.FromResult(SimsCommandHandlerResult.Failed(decision.RejectionReason ?? decision.Message));
        }

        var variant = string.IsNullOrWhiteSpace(request.GuestVariant) ? SimsGuestVariants.RandomGuest : request.GuestVariant;
        var guestName = string.IsNullOrWhiteSpace(request.GuestName) ? "a surprise guest" : request.GuestName;
        var venue = string.IsNullOrWhiteSpace(request.Venue) ? "the lot" : request.Venue;
        var arrival = request.AnnounceArrival ? "arrives with a knock" : "shows up unexpectedly";
        var variantLabel = variant switch
        {
            SimsGuestVariants.Friend => "friendly",
            SimsGuestVariants.Rival => "rival",
            SimsGuestVariants.Neighbor => "neighbor",
            _ => "random",
        };

        var message = $"{guestName} ({variantLabel}) {arrival} at {venue}.";
        return Task.FromResult(SimsCommandHandlerResult.Processed(message));
    }
}


public static class SimsCommandQueueReader
{
    public static async Task<IReadOnlyList<SimsCommand>> ReadAsync(string queuePath, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(queuePath)
            ? "command-queue.jsonl"
            : queuePath;

        if (!File.Exists(path))
        {
            return Array.Empty<SimsCommand>();
        }

        var commands = new List<SimsCommand>();
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var command = JsonSerializer.Deserialize<SimsCommand>(line);
                if (command is not null)
                {
                    commands.Add(command);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Unable to parse command queue line {index + 1} from '{path}'.", ex);
            }
        }

        return commands;
    }

    public static Task<string?> ClaimAsync(string queuePath, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(queuePath)
            ? "command-queue.jsonl"
            : queuePath;

        if (!File.Exists(path))
        {
            return Task.FromResult<string?>(null);
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        Directory.CreateDirectory(directory);

        var claimPath = Path.Combine(
            directory,
            $"{Path.GetFileNameWithoutExtension(path)}.{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.{Guid.NewGuid():N}.processing.jsonl");

        try
        {
            File.Move(path, claimPath);
            return Task.FromResult<string?>(claimPath);
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult<string?>(null);
        }
        catch (DirectoryNotFoundException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public static IReadOnlyList<string> GetClaimedPaths(string queuePath)
    {
        var path = string.IsNullOrWhiteSpace(queuePath)
            ? "command-queue.jsonl"
            : queuePath;

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(directory, $"{Path.GetFileNameWithoutExtension(path)}.*.processing.jsonl")
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static Task ReleaseAsync(string claimedPath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(claimedPath) && File.Exists(claimedPath))
        {
            File.Delete(claimedPath);
        }

        return Task.CompletedTask;
    }
}

public sealed class SimsModRuntime(SimsCommandDispatcher dispatcher, string queuePath = "command-queue.jsonl")
{
    private readonly SimsCommandDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly string _queuePath = string.IsNullOrWhiteSpace(queuePath) ? "command-queue.jsonl" : queuePath;

    public async Task<IReadOnlyList<SimsCommandDispatchRecord>> ProcessPendingCommandsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<SimsCommandDispatchRecord>();
        var claimPaths = new List<string>(SimsCommandQueueReader.GetClaimedPaths(_queuePath));

        var claimPath = await SimsCommandQueueReader.ClaimAsync(_queuePath, cancellationToken);
        if (!string.IsNullOrWhiteSpace(claimPath))
        {
            claimPaths.Add(claimPath);
        }

        claimPaths.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (var path in claimPaths)
        {
            var commands = await SimsCommandQueueReader.ReadAsync(path, cancellationToken);
            foreach (var command in commands)
            {
                var latest = await _dispatcher.ReadLatestAsync(command.Id, cancellationToken);
                if (latest is { Status: SimsCommandDispatchStatus.Processed or SimsCommandDispatchStatus.Failed or SimsCommandDispatchStatus.Unhandled })
                {
                    continue;
                }

                results.Add(await _dispatcher.DispatchAsync(command, cancellationToken));
            }

            await SimsCommandQueueReader.ReleaseAsync(path, cancellationToken);
        }

        return results;
    }
}

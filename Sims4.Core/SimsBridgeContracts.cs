namespace PolyhydraGames.Sims4.Core;

public static class SimsActionNames
{
    public const string AddFunds = "add_funds";
    public const string AddBuff = "add_buff";
    public const string SendNotification = "send_notification";
    public const string RunInteraction = "run_interaction";
    public const string SpawnObject = "spawn_object";
    public const string AddItem = "add_item";
    public const string TakeItem = "take_item";
    public const string TakeStock = "take_stock";
    public const string TriggerInfluence = "trigger_influence";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        AddFunds,
        AddBuff,
        SendNotification,
        RunInteraction,
        SpawnObject,
        AddItem,
        TakeItem,
        TakeStock,
        TriggerInfluence,
    };
}

public static class SimsInfluenceKinds
{
    public const string FireIncident = "fire_incident";
    public const string RobberBreakIn = "robber_break_in";
    public const string SurpriseGuest = "surprise_guest";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        FireIncident,
        RobberBreakIn,
        SurpriseGuest,
    };
}

public static class SimsEventNames
{
    public const string Heartbeat = "Heartbeat";
    public const string Capabilities = "Capabilities";
    public const string InventorySnapshot = "InventorySnapshot";
    public const string CommandQueued = "CommandQueued";
    public const string CommandCompleted = "CommandCompleted";
    public const string CommandFailed = "CommandFailed";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Heartbeat,
        Capabilities,
        InventorySnapshot,
        CommandQueued,
        CommandCompleted,
        CommandFailed,
    };
}

public sealed record SimsCommand(
    string StreamerUserId,
    string Action,
    DateTimeOffset Timestamp,
    object Payload,
    string Id,
    SimsTarget? Target = null,
    string? CorrelationId = null
);

public sealed record SimsTarget(
    string? SimId = null,
    string? HouseholdId = null,
    string? ObjectId = null,
    string? InventoryScope = null
);

public sealed record SimsInventoryItem(
    string ItemId,
    string DisplayName,
    int Quantity,
    bool IsStackable,
    string? Category = null,
    string? Source = null
);

public sealed record SimsInventorySnapshot(
    string StreamerUserId,
    DateTimeOffset CapturedAt,
    SimsTarget? Target,
    IReadOnlyList<SimsInventoryItem> Items,
    int? HouseholdFunds = null,
    string? Notes = null
);

public record SimsInfluenceRequest(
    string Kind,
    DateTimeOffset RequestedAt,
    string? Reason = null,
    SimsTarget? Target = null,
    bool RequiresApproval = false,
    string? ApprovalToken = null);

public sealed record SimsFireIncidentRequest(
    DateTimeOffset RequestedAt,
    string? Reason = null,
    SimsTarget? Target = null,
    bool RequiresApproval = true,
    string? ApprovalToken = null,
    string? Room = null,
    string Severity = "medium",
    bool NotifyFirefighters = true)
    : SimsInfluenceRequest(
        SimsInfluenceKinds.FireIncident,
        RequestedAt,
        Reason,
        Target,
        RequiresApproval,
        ApprovalToken);

public static class SimsGuestVariants
{
    public const string Friend = "friend";
    public const string Rival = "rival";
    public const string Neighbor = "neighbor";
    public const string RandomGuest = "random_guest";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        Friend,
        Rival,
        Neighbor,
        RandomGuest,
    };
}

public sealed record SimsRobberBreakInRequest(
    DateTimeOffset RequestedAt,
    string? Reason = null,
    SimsTarget? Target = null,
    bool RequiresApproval = true,
    string? ApprovalToken = null,
    string? EntryPoint = null,
    string Severity = "medium",
    bool Stealthy = true,
    bool PoliceNotified = true)
    : SimsInfluenceRequest(
        SimsInfluenceKinds.RobberBreakIn,
        RequestedAt,
        Reason,
        Target,
        RequiresApproval,
        ApprovalToken);

public sealed record SimsSurpriseGuestRequest(
    DateTimeOffset RequestedAt,
    string? Reason = null,
    SimsTarget? Target = null,
    string GuestVariant = SimsGuestVariants.RandomGuest,
    string? GuestName = null,
    string? Venue = null,
    bool AnnounceArrival = true,
    bool RequiresApproval = false,
    string? ApprovalToken = null)
    : SimsInfluenceRequest(
        SimsInfluenceKinds.SurpriseGuest,
        RequestedAt,
        Reason,
        Target,
        RequiresApproval,
        ApprovalToken);

public sealed record SimsModCapabilities(
    string ModVersion,
    string ApiVersion,
    IReadOnlyCollection<string> SupportedActions,
    IReadOnlyCollection<string> SupportedEvents,
    bool SupportsInventoryExposure,
    bool SupportsStocktake
);

public sealed record SimsCapabilitySnapshot(
    DateTimeOffset CapturedAt,
    SimsModCapabilities Capabilities
);

public static class SimsCapabilitySnapshotFactory
{
    public static SimsCapabilitySnapshot Create(DateTimeOffset capturedAt, string modVersion, string apiVersion)
    {
        var supportedActions = SimsActionNames.All
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var supportedEvents = SimsEventNames.All
            .OrderBy(evt => evt, StringComparer.Ordinal)
            .ToArray();

        return new SimsCapabilitySnapshot(
            capturedAt,
            new SimsModCapabilities(
                modVersion,
                apiVersion,
                supportedActions,
                supportedEvents,
                SupportsInventoryExposure: true,
                SupportsStocktake: true));
    }
}

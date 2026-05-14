namespace CAAutomationHub.Contracts.Runtime;

public sealed record RuntimeSnapshot
{
    public RuntimeSnapshot(
        DateTimeOffset capturedAt,
        RuntimeHealthState? health,
        IReadOnlyList<ChannelRuntimeState>? channels,
        IReadOnlyList<RuntimeEvent>? recentEvents)
    {
        CapturedAt = capturedAt;
        Health = health ?? RuntimeHealthState.Empty;
        Channels = channels ?? Array.Empty<ChannelRuntimeState>();
        RecentEvents = recentEvents ?? Array.Empty<RuntimeEvent>();
    }

    public DateTimeOffset CapturedAt { get; init; }
    public RuntimeHealthState Health { get; init; }
    public IReadOnlyList<ChannelRuntimeState> Channels { get; init; }
    public IReadOnlyList<RuntimeEvent> RecentEvents { get; init; }

    public static RuntimeSnapshot Empty { get; } = new(
        capturedAt: DateTimeOffset.UnixEpoch,
        health: RuntimeHealthState.Empty,
        channels: Array.Empty<ChannelRuntimeState>(),
        recentEvents: Array.Empty<RuntimeEvent>());
}

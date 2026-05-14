using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Mappers;

public static class RuntimeDashboardSnapshotMapper
{
    private const string RuntimeEventSource = "Runtime";

    public static DashboardSnapshot Map(RuntimeSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var cards = source.Channels
            .Select(MapChannel)
            .ToArray();

        return new DashboardSnapshot(
            MapHealth(source.Health),
            cards,
            CommunicationTrendSetSnapshot.Empty);
    }

    public static RuntimeHealthSnapshot MapHealth(RuntimeHealthState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new RuntimeHealthSnapshot(
            TotalPlcs: source.TotalPlcs,
            HealthyCount: source.HealthyCount,
            WarningCount: source.WarningCount,
            CongestedCount: source.CongestedCount,
            ErrorCount: source.ErrorCount,
            SnapshotTime: source.CapturedAt,
            InactiveCount: source.InactiveCount);
    }

    public static PlcCardSnapshot MapChannel(ChannelRuntimeState source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new PlcCardSnapshot(
            plcId: source.PlcId,
            plcName: source.PlcName,
            lineName: source.LineName,
            connectionState: MapConnectionState(source.HealthSeverity),
            ipAddress: source.IpAddress,
            port: source.Port,
            pollingIntervalMs: GetPollingIntervalMs(source),
            lastResponseMs: source.LastResponseMs,
            txPerMinute: 0,
            rxPerMinute: 0,
            // Runtime currently exposes consecutive failures, not a cumulative error count.
            errorCount: source.ConsecutiveFailures,
            runtimeSignal: CreateRuntimeSignalFallback(source.SequenceState));
    }

    public static PlcConnectionState MapConnectionState(PlcHealthSeverity severity)
        => severity switch
        {
            PlcHealthSeverity.Healthy => PlcConnectionState.Healthy,
            PlcHealthSeverity.Warning => PlcConnectionState.Warning,
            PlcHealthSeverity.Congested => PlcConnectionState.Congested,
            PlcHealthSeverity.Error => PlcConnectionState.Error,
            PlcHealthSeverity.Inactive => PlcConnectionState.Inactive,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };

    public static RuntimeDashboardEvent MapEvent(RuntimeEvent source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var sourceName = string.IsNullOrWhiteSpace(source.PlcId)
            ? RuntimeEventSource
            : source.PlcId;

        return new RuntimeDashboardEvent(
            OccurredAt: source.OccurredAt,
            Severity: source.Severity.ToString(),
            Message: source.Message,
            Source: sourceName,
            Category: source.Category.ToString(),
            PlcId: source.PlcId,
            PlcName: null,
            Status: source.Status);
    }

    public static IReadOnlyList<RuntimeDashboardEvent> MapEvents(IEnumerable<RuntimeEvent> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(MapEvent).ToArray();
    }

    public static IReadOnlyList<RuntimeDashboardEvent> MapEvents(RuntimeSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return MapEvents(source.RecentEvents);
    }

    private static int GetPollingIntervalMs(ChannelRuntimeState source)
        => source.EffectivePollingIntervalMs > 0
            ? source.EffectivePollingIntervalMs
            : source.ConfiguredPollingIntervalMs;

    private static PlcRuntimeSignalSnapshot CreateRuntimeSignalFallback(RuntimeSequenceState state)
        => new(
            GetSequenceName(state),
            MapSequenceStatus(state),
            TimeSpan.Zero,
            LastSequenceMessage: null,
            ResponseLatencyBuckets: Array.Empty<SequenceResponseLatencyBucket>());

    private static RuntimeSequenceStatus MapSequenceStatus(RuntimeSequenceState state)
        => state switch
        {
            RuntimeSequenceState.Idle => RuntimeSequenceStatus.Idle,
            RuntimeSequenceState.Running => RuntimeSequenceStatus.Running,
            RuntimeSequenceState.Waiting => RuntimeSequenceStatus.Waiting,
            RuntimeSequenceState.Delayed => RuntimeSequenceStatus.Delayed,
            RuntimeSequenceState.Failed => RuntimeSequenceStatus.Failed,
            RuntimeSequenceState.Completed => RuntimeSequenceStatus.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

    private static string GetSequenceName(RuntimeSequenceState state)
        => state switch
        {
            RuntimeSequenceState.Idle => "대기",
            RuntimeSequenceState.Running => "실행 중",
            RuntimeSequenceState.Waiting => "대기 중",
            RuntimeSequenceState.Delayed => "지연",
            RuntimeSequenceState.Failed => "실패",
            RuntimeSequenceState.Completed => "완료",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
}

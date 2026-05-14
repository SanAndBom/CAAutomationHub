using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Wpf.Mappers;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Tests.Mappers;

public sealed class RuntimeDashboardSnapshotMapperTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset OccurredAt = new(2026, 5, 14, 12, 1, 0, TimeSpan.Zero);

    [Fact]
    public void Map_ThrowsArgumentNullExceptionWhenSnapshotIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => RuntimeDashboardSnapshotMapper.Map(null!));
    }

    [Fact]
    public void Map_ConvertsEmptyRuntimeSnapshotToNullSafeDashboardSnapshot()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(RuntimeSnapshot.Empty);

        Assert.NotNull(snapshot.Health);
        Assert.Empty(snapshot.PlcCards);
        Assert.NotNull(snapshot.CommunicationTrend);
        Assert.Same(CommunicationTrendSetSnapshot.Empty, snapshot.CommunicationTrend);
    }

    [Fact]
    public void Map_MapsRuntimeHealthCountsAndCapturedAt()
    {
        var runtimeHealth = new RuntimeHealthState(
            TotalPlcs: 7,
            OnlineCount: 6,
            ReconnectingCount: 1,
            HealthyCount: 3,
            WarningCount: 1,
            CongestedCount: 1,
            ErrorCount: 1,
            InactiveCount: 1,
            CapturedAt: CapturedAt);

        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(new RuntimeSnapshot(
            CapturedAt,
            runtimeHealth,
            channels: [],
            recentEvents: []));

        Assert.Equal(7, snapshot.Health.TotalPlcs);
        Assert.Equal(3, snapshot.Health.HealthyCount);
        Assert.Equal(1, snapshot.Health.WarningCount);
        Assert.Equal(1, snapshot.Health.CongestedCount);
        Assert.Equal(1, snapshot.Health.ErrorCount);
        Assert.Equal(1, snapshot.Health.InactiveCount);
        Assert.Equal(CapturedAt, snapshot.Health.SnapshotTime);
    }

    [Theory]
    [InlineData(PlcHealthSeverity.Healthy, PlcConnectionState.Healthy)]
    [InlineData(PlcHealthSeverity.Warning, PlcConnectionState.Warning)]
    [InlineData(PlcHealthSeverity.Congested, PlcConnectionState.Congested)]
    [InlineData(PlcHealthSeverity.Error, PlcConnectionState.Error)]
    [InlineData(PlcHealthSeverity.Inactive, PlcConnectionState.Inactive)]
    public void Map_MapsAllPlcHealthSeverities(
        PlcHealthSeverity healthSeverity,
        PlcConnectionState expectedConnectionState)
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(
            CreateChannel(healthSeverity: healthSeverity)));

        Assert.Equal(expectedConnectionState, snapshot.PlcCards.Single().ConnectionState);
    }

    [Fact]
    public void Map_MapsChannelRuntimeStateBasicFields()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel(
            plcId: "PLC-02",
            plcName: "Press PLC",
            lineName: "Line-B",
            ipAddress: "10.0.0.42",
            port: 2100,
            effectivePollingIntervalMs: 250,
            lastResponseMs: 37,
            consecutiveFailures: 4)));

        PlcCardSnapshot card = snapshot.PlcCards.Single();
        Assert.Equal("PLC-02", card.PlcId);
        Assert.Equal("Press PLC", card.PlcName);
        Assert.Equal("Line-B", card.LineName);
        Assert.Equal("10.0.0.42", card.IpAddress);
        Assert.Equal(2100, card.Port);
        Assert.Equal(250, card.PollingIntervalMs);
        Assert.Equal(37, card.LastResponseMs);
        Assert.Equal(0, card.TxPerMinute);
        Assert.Equal(0, card.RxPerMinute);
        Assert.Equal(4, card.ErrorCount);
    }

    [Fact]
    public void Map_PreservesChannelCountAsPlcCardCount()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(
            CreateChannel(plcId: "PLC-01"),
            CreateChannel(plcId: "PLC-02"),
            CreateChannel(plcId: "PLC-03")));

        Assert.Equal(3, snapshot.PlcCards.Count);
    }

    [Fact]
    public void Map_UsesEffectivePollingIntervalWhenPositive()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel(
            configuredPollingIntervalMs: 1000,
            effectivePollingIntervalMs: 350)));

        Assert.Equal(350, snapshot.PlcCards.Single().PollingIntervalMs);
    }

    [Fact]
    public void Map_UsesConfiguredPollingIntervalWhenEffectiveIsZero()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel(
            configuredPollingIntervalMs: 1000,
            effectivePollingIntervalMs: 0)));

        Assert.Equal(1000, snapshot.PlcCards.Single().PollingIntervalMs);
    }

    [Fact]
    public void Map_UsesConsecutiveFailuresAsTemporaryErrorCount()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel(
            consecutiveFailures: 9)));

        Assert.Equal(9, snapshot.PlcCards.Single().ErrorCount);
    }

    [Fact]
    public void Map_AlwaysProvidesRuntimeSignalFallback()
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel()));

        Assert.NotNull(snapshot.PlcCards.Single().RuntimeSignal);
    }

    [Theory]
    [InlineData(RuntimeSequenceState.Idle, RuntimeSequenceStatus.Idle, "대기")]
    [InlineData(RuntimeSequenceState.Running, RuntimeSequenceStatus.Running, "실행 중")]
    [InlineData(RuntimeSequenceState.Waiting, RuntimeSequenceStatus.Waiting, "대기 중")]
    [InlineData(RuntimeSequenceState.Delayed, RuntimeSequenceStatus.Delayed, "지연")]
    [InlineData(RuntimeSequenceState.Failed, RuntimeSequenceStatus.Failed, "실패")]
    [InlineData(RuntimeSequenceState.Completed, RuntimeSequenceStatus.Completed, "완료")]
    public void Map_MapsAllRuntimeSequenceStatesToSignalFallback(
        RuntimeSequenceState sequenceState,
        RuntimeSequenceStatus expectedStatus,
        string expectedName)
    {
        DashboardSnapshot snapshot = RuntimeDashboardSnapshotMapper.Map(CreateSnapshot(CreateChannel(
            sequenceState: sequenceState)));

        PlcRuntimeSignalSnapshot signal = snapshot.PlcCards.Single().RuntimeSignal;
        Assert.Equal(expectedName, signal.CurrentSequenceName);
        Assert.Equal(expectedStatus, signal.CurrentSequenceStatus);
        Assert.Equal(TimeSpan.Zero, signal.CurrentSequenceElapsed);
        Assert.Empty(signal.ResponseLatencyBuckets);
    }

    [Fact]
    public void MapEvent_MapsRuntimeEventToDashboardEvent()
    {
        var runtimeEvent = new RuntimeEvent(
            EventId: "evt-001",
            OccurredAt: OccurredAt,
            PlcId: "PLC-01",
            Severity: RuntimeEventSeverity.Warning,
            Category: RuntimeEventCategory.Polling,
            Message: "Polling delayed.",
            Status: "Watch",
            Detail: "detail is not mapped yet");

        RuntimeDashboardEvent dashboardEvent = RuntimeDashboardSnapshotMapper.MapEvent(runtimeEvent);

        Assert.Equal(OccurredAt, dashboardEvent.OccurredAt);
        Assert.Equal("Warning", dashboardEvent.Severity);
        Assert.Equal("Polling delayed.", dashboardEvent.Message);
        Assert.Equal("PLC-01", dashboardEvent.Source);
        Assert.Equal("Polling", dashboardEvent.Category);
        Assert.Equal("PLC-01", dashboardEvent.PlcId);
        Assert.Null(dashboardEvent.PlcName);
        Assert.Equal("Watch", dashboardEvent.Status);
    }

    [Fact]
    public void MapEvent_UsesRuntimeSourceWhenPlcIdIsMissing()
    {
        var runtimeEvent = new RuntimeEvent(
            EventId: "evt-002",
            OccurredAt: OccurredAt,
            PlcId: null,
            Severity: RuntimeEventSeverity.Info,
            Category: RuntimeEventCategory.General,
            Message: "Runtime started.",
            Status: null,
            Detail: null);

        RuntimeDashboardEvent dashboardEvent = RuntimeDashboardSnapshotMapper.MapEvent(runtimeEvent);

        Assert.Equal("Runtime", dashboardEvent.Source);
        Assert.Null(dashboardEvent.PlcId);
    }

    [Fact]
    public void MapEvents_MapsSnapshotRecentEvents()
    {
        var first = CreateEvent("evt-001");
        var second = CreateEvent("evt-002");
        var snapshot = new RuntimeSnapshot(CapturedAt, RuntimeHealthState.Empty, channels: [], [first, second]);

        IReadOnlyList<RuntimeDashboardEvent> dashboardEvents = RuntimeDashboardSnapshotMapper.MapEvents(snapshot);

        Assert.Equal(2, dashboardEvents.Count);
        Assert.Equal("evt-001 message", dashboardEvents[0].Message);
        Assert.Equal("evt-002 message", dashboardEvents[1].Message);
    }

    private static RuntimeSnapshot CreateSnapshot(params ChannelRuntimeState[] channels)
        => new(CapturedAt, RuntimeHealthState.Empty, channels, recentEvents: []);

    private static ChannelRuntimeState CreateChannel(
        string plcId = "PLC-01",
        string plcName = "Cutting PLC",
        string lineName = "Line-A",
        PlcHealthSeverity healthSeverity = PlcHealthSeverity.Healthy,
        string ipAddress = "192.168.0.10",
        int port = 2004,
        RuntimeSequenceState sequenceState = RuntimeSequenceState.Idle,
        int configuredPollingIntervalMs = 500,
        int effectivePollingIntervalMs = 500,
        int lastResponseMs = 12,
        int consecutiveFailures = 0)
        => new(
            PlcId: plcId,
            PlcName: plcName,
            LineName: lineName,
            IsEnabled: true,
            IpAddress: ipAddress,
            Port: port,
            LinkState: PlcLinkState.Online,
            HealthSeverity: healthSeverity,
            PollingState: PlcPollingState.Polling,
            SequenceState: sequenceState,
            ConfiguredPollingIntervalMs: configuredPollingIntervalMs,
            EffectivePollingIntervalMs: effectivePollingIntervalMs,
            LastResponseMs: lastResponseMs,
            ConsecutiveFailures: consecutiveFailures,
            ReconnectCount: 0,
            SuccessRate: 1.0,
            LastSuccessAt: CapturedAt,
            LastFailureAt: null,
            LastError: null);

    private static RuntimeEvent CreateEvent(string eventId)
        => new(
            EventId: eventId,
            OccurredAt: OccurredAt,
            PlcId: "PLC-01",
            Severity: RuntimeEventSeverity.Info,
            Category: RuntimeEventCategory.General,
            Message: $"{eventId} message",
            Status: "Live",
            Detail: null);
}

using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Tests.Adapters;

public sealed class RuntimeDashboardAdapterTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DefaultConstructor_ReturnsNullSafeDashboardSnapshotFromEmptyRuntimeSnapshot()
    {
        var adapter = new RuntimeDashboardAdapter();

        DashboardSnapshot snapshot = adapter.GetSnapshot();

        Assert.NotNull(snapshot.Health);
        Assert.Empty(snapshot.PlcCards);
        Assert.NotNull(snapshot.CommunicationTrend);
        Assert.Same(CommunicationTrendSetSnapshot.Empty, snapshot.CommunicationTrend);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullExceptionWhenProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new RuntimeDashboardAdapter(null!));
    }

    [Fact]
    public void GetSnapshot_MapsRuntimeHealthCountsThroughMapper()
    {
        var provider = new StubRuntimeSnapshotProvider(new RuntimeSnapshot(
            CapturedAt,
            new RuntimeHealthState(
                TotalPlcs: 7,
                OnlineCount: 6,
                ReconnectingCount: 1,
                HealthyCount: 3,
                WarningCount: 1,
                CongestedCount: 1,
                ErrorCount: 1,
                InactiveCount: 1,
                CapturedAt: CapturedAt),
            channels: [],
            recentEvents: []));
        var adapter = new RuntimeDashboardAdapter(provider);

        DashboardSnapshot snapshot = adapter.GetSnapshot();

        Assert.Equal(7, snapshot.Health.TotalPlcs);
        Assert.Equal(3, snapshot.Health.HealthyCount);
        Assert.Equal(1, snapshot.Health.WarningCount);
        Assert.Equal(1, snapshot.Health.CongestedCount);
        Assert.Equal(1, snapshot.Health.ErrorCount);
        Assert.Equal(1, snapshot.Health.InactiveCount);
        Assert.Equal(CapturedAt, snapshot.Health.SnapshotTime);
    }

    [Fact]
    public void GetSnapshot_MapsChannelRuntimeStateToPlcCardSnapshot()
    {
        var provider = new StubRuntimeSnapshotProvider(new RuntimeSnapshot(
            CapturedAt,
            RuntimeHealthState.Empty,
            [CreateChannel()],
            recentEvents: []));
        var adapter = new RuntimeDashboardAdapter(provider);

        PlcCardSnapshot card = adapter.GetSnapshot().PlcCards.Single();

        Assert.Equal("PLC-02", card.PlcId);
        Assert.Equal("Press PLC", card.PlcName);
        Assert.Equal("Line-B", card.LineName);
        Assert.Equal(PlcConnectionState.Warning, card.ConnectionState);
        Assert.Equal("10.0.0.42", card.IpAddress);
        Assert.Equal(2100, card.Port);
        Assert.Equal(250, card.PollingIntervalMs);
        Assert.Equal(37, card.LastResponseMs);
        Assert.Equal(4, card.ErrorCount);
    }

    [Fact]
    public void GetSnapshot_ProvidesNonNullCommunicationTrendWhenProviderReturnsEmptyRuntimeSnapshot()
    {
        var adapter = new RuntimeDashboardAdapter(new StubRuntimeSnapshotProvider(RuntimeSnapshot.Empty));

        DashboardSnapshot snapshot = adapter.GetSnapshot();

        Assert.NotNull(snapshot.CommunicationTrend);
        Assert.Same(CommunicationTrendSetSnapshot.Empty, snapshot.CommunicationTrend);
    }

    [Fact]
    public void GetSnapshot_PropagatesProviderException()
    {
        var expected = new InvalidOperationException("provider failed");
        var adapter = new RuntimeDashboardAdapter(new ThrowingRuntimeSnapshotProvider(expected));

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => adapter.GetSnapshot());

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetSnapshot_CallsProviderOncePerSnapshot()
    {
        var provider = new StubRuntimeSnapshotProvider(RuntimeSnapshot.Empty);
        var adapter = new RuntimeDashboardAdapter(provider);

        adapter.GetSnapshot();

        Assert.Equal(1, provider.CallCount);
    }

    private static ChannelRuntimeState CreateChannel()
        => new(
            PlcId: "PLC-02",
            PlcName: "Press PLC",
            LineName: "Line-B",
            IsEnabled: true,
            IpAddress: "10.0.0.42",
            Port: 2100,
            LinkState: PlcLinkState.Online,
            HealthSeverity: PlcHealthSeverity.Warning,
            PollingState: PlcPollingState.Polling,
            SequenceState: RuntimeSequenceState.Running,
            ConfiguredPollingIntervalMs: 500,
            EffectivePollingIntervalMs: 250,
            LastResponseMs: 37,
            ConsecutiveFailures: 4,
            ReconnectCount: 0,
            SuccessRate: 0.99,
            LastSuccessAt: CapturedAt,
            LastFailureAt: null,
            LastError: null);

    private sealed class StubRuntimeSnapshotProvider : IRuntimeSnapshotProvider
    {
        private readonly RuntimeSnapshot _snapshot;

        public StubRuntimeSnapshotProvider(RuntimeSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int CallCount { get; private set; }

        public RuntimeSnapshot GetSnapshot()
        {
            CallCount++;
            return _snapshot;
        }
    }

    private sealed class ThrowingRuntimeSnapshotProvider : IRuntimeSnapshotProvider
    {
        private readonly Exception _exception;

        public ThrowingRuntimeSnapshotProvider(Exception exception)
        {
            _exception = exception;
        }

        public RuntimeSnapshot GetSnapshot() => throw _exception;
    }
}

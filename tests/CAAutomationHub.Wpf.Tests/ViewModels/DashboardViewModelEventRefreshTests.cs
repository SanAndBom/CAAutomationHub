using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;
using CAAutomationHub.Wpf.ViewModels;

namespace CAAutomationHub.Wpf.Tests.ViewModels;

public sealed class DashboardViewModelEventRefreshTests
{
    [Fact]
    public void Constructor_SubscribesToSnapshotChangedWhenAdapterIsEventSource()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var adapter = new TestEventSourceAdapter(initial);

        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        Assert.Equal(1, adapter.SnapshotChangedHandlerCount);
    }

    [Fact]
    public void SnapshotChanged_AppliesEventPayloadSnapshot()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var pushed = CreateSnapshot(2, "PLC-02");
        var adapter = new TestEventSourceAdapter(initial);
        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        adapter.RaiseSnapshotChanged(pushed);

        Assert.Single(viewModel.PlcCards);
        Assert.Equal("PLC-02", viewModel.PlcCards[0].PlcId);
        Assert.Equal(2, viewModel.TotalCount);
        Assert.Equal(2, viewModel.HealthyCount);
    }

    [Fact]
    public void SnapshotChanged_UsesEventPayloadWithoutPullingSnapshotAgain()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var pushed = CreateSnapshot(2, "PLC-02");
        var adapter = new TestEventSourceAdapter(initial);
        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        var callsAfterInitialLoad = adapter.GetSnapshotCallCount;
        adapter.RaiseSnapshotChanged(pushed);

        Assert.Equal(callsAfterInitialLoad, adapter.GetSnapshotCallCount);
        Assert.Equal("PLC-02", viewModel.PlcCards[0].PlcId);
    }

    [Fact]
    public void EventSourceAdapter_StillSupportsPullRefresh()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var pulled = CreateSnapshot(2, "PLC-02");
        var adapter = new TestEventSourceAdapter(initial);
        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        adapter.NextSnapshot = pulled;
        viewModel.RefreshCommand.Execute(null);

        Assert.Equal(2, adapter.GetSnapshotCallCount);
        Assert.Equal("PLC-02", viewModel.PlcCards[0].PlcId);
    }

    [Fact]
    public void Dispose_UnsubscribesSnapshotChangedAndIgnoresLaterEvents()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var pushed = CreateSnapshot(2, "PLC-02");
        var adapter = new TestEventSourceAdapter(initial);
        var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        viewModel.Dispose();
        adapter.RaiseSnapshotChanged(pushed);

        Assert.Equal(0, adapter.SnapshotChangedHandlerCount);
        Assert.Single(viewModel.PlcCards);
        Assert.Equal("PLC-01", viewModel.PlcCards[0].PlcId);
    }

    [Fact]
    public void Constructor_DoesNotSubscribeToEventReceived()
    {
        var adapter = new TestEventSourceAdapter(CreateSnapshot(1, "PLC-01"));

        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        Assert.Equal(0, adapter.EventReceivedHandlerCount);
    }

    [Fact]
    public void ManualPullRefresh_PreventsOlderPushSnapshotFromOverwritingView()
    {
        var initial = CreateSnapshot(1, "PLC-01");
        var newerPull = CreateSnapshot(3, "PLC-03");
        var olderPush = CreateSnapshot(2, "PLC-02");
        var adapter = new TestEventSourceAdapter(initial);
        using var viewModel = new DashboardViewModel(adapter, null, new ImmediateUiDispatcher());

        adapter.NextSnapshot = newerPull;
        viewModel.RefreshCommand.Execute(null);
        adapter.RaiseSnapshotChanged(olderPush);

        Assert.Single(viewModel.PlcCards);
        Assert.Equal("PLC-03", viewModel.PlcCards[0].PlcId);
        Assert.Equal(3, viewModel.TotalCount);
    }

    private static DashboardSnapshot CreateSnapshot(int second, string plcId)
    {
        var card = new PlcCardSnapshot(
            plcId,
            $"{plcId} Name",
            "Line-1",
            PlcConnectionState.Healthy,
            "192.168.0.10",
            2004,
            500,
            20,
            100,
            98,
            0);
        var health = new RuntimeHealthSnapshot(
            TotalPlcs: second,
            HealthyCount: second,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            SnapshotTime: new DateTimeOffset(2026, 5, 14, 12, 0, second, TimeSpan.Zero));
        var overview = new CommunicationTrendSnapshot(
            "overview",
            "Overview",
            isOverview: true,
            WarningThresholdMs: 100,
            ErrorThresholdMs: 500,
            points: [],
            series: []);

        return new DashboardSnapshot(
            health,
            [card],
            new CommunicationTrendSetSnapshot(overview, []));
    }

    private sealed class TestEventSourceAdapter : IRuntimeDashboardAdapter, IRuntimeDashboardEventSource
    {
        private DashboardSnapshot _snapshot;
        private EventHandler<DashboardSnapshotChangedEventArgs>? _snapshotChanged;
        private EventHandler<RuntimeDashboardEvent>? _eventReceived;

        public TestEventSourceAdapter(DashboardSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public DashboardSnapshot NextSnapshot
        {
            set => _snapshot = value;
        }

        public int GetSnapshotCallCount { get; private set; }

        public int SnapshotChangedHandlerCount => _snapshotChanged?.GetInvocationList().Length ?? 0;

        public int EventReceivedHandlerCount => _eventReceived?.GetInvocationList().Length ?? 0;

        public event EventHandler<DashboardSnapshotChangedEventArgs>? SnapshotChanged
        {
            add => _snapshotChanged += value;
            remove => _snapshotChanged -= value;
        }

        public event EventHandler<RuntimeDashboardEvent>? EventReceived
        {
            add => _eventReceived += value;
            remove => _eventReceived -= value;
        }

        public DashboardSnapshot GetSnapshot()
        {
            GetSnapshotCallCount++;
            return _snapshot;
        }

        public void RaiseSnapshotChanged(DashboardSnapshot snapshot)
            => _snapshotChanged?.Invoke(
                this,
                new DashboardSnapshotChangedEventArgs(snapshot, snapshot.Health.SnapshotTime));
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }
}

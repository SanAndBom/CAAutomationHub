using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Tests;

public sealed class InMemoryAutomationHubSupervisorChannelRegistryTests
{
    [Fact]
    public void Constructor_ThrowsWhenRuntimeChannelRegistryIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new InMemoryAutomationHubSupervisor(null!));

        Assert.Equal("channelRegistry", exception.ParamName);
    }

    [Fact]
    public async Task StartAsync_WithDefaultConstructorPublishesSnapshotForEmptyRegistry()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        RuntimeSnapshotChangedEventArgs? change = null;
        supervisor.SnapshotChanged += (_, args) => change = args;

        await supervisor.StartAsync(CancellationToken.None);

        Assert.NotNull(change);
        Assert.Empty(change.Snapshot.Channels);
        Assert.Equal(0, change.Snapshot.Health.TotalPlcs);
        Assert.Equal(change.Snapshot.CapturedAt, change.Snapshot.Health.CapturedAt);
    }

    [Fact]
    public async Task StartAsync_PublishesRegistryChannelStatesInSnapshotAndEvent()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            lineName: "Line-A",
            ipAddress: "192.168.0.10",
            port: 2004,
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Healthy));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        RuntimeSnapshotChangedEventArgs? change = null;
        supervisor.SnapshotChanged += (_, args) => change = args;

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.NotNull(change);
        Assert.Same(snapshot, change.Snapshot);
        ChannelRuntimeState channel = Assert.Single(snapshot.Channels);
        Assert.Equal("PLC-01", channel.PlcId);
        Assert.Equal("Cutting PLC", channel.PlcName);
        Assert.Equal("Line-A", channel.LineName);
        Assert.Equal("192.168.0.10", channel.IpAddress);
        Assert.Equal(2004, channel.Port);
        Assert.Equal(snapshot.CapturedAt, snapshot.Health.CapturedAt);
        Assert.Equal(snapshot.CapturedAt, change.OccurredAt);
    }

    [Fact]
    public async Task StartAsync_CalculatesMinimalRuntimeHealthFromRegistryChannels()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Healthy PLC",
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Healthy));
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-02",
            plcName: "Warning PLC",
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning));
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-03",
            plcName: "Congested PLC",
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Congested));
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-04",
            plcName: "Error PLC",
            linkState: PlcLinkState.Faulted,
            healthSeverity: PlcHealthSeverity.Error));
        registry.Add(new InMemoryRuntimePlcChannel(
            plcId: "PLC-05",
            plcName: "Inactive PLC",
            linkState: PlcLinkState.Offline,
            healthSeverity: PlcHealthSeverity.Inactive));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(5, snapshot.Health.TotalPlcs);
        Assert.Equal(2, snapshot.Health.OnlineCount);
        Assert.Equal(1, snapshot.Health.ReconnectingCount);
        Assert.Equal(1, snapshot.Health.HealthyCount);
        Assert.Equal(1, snapshot.Health.WarningCount);
        Assert.Equal(1, snapshot.Health.CongestedCount);
        Assert.Equal(1, snapshot.Health.ErrorCount);
        Assert.Equal(1, snapshot.Health.InactiveCount);
    }

    [Fact]
    public async Task StartAsync_PreservesChannelEventTimestampsWhileUsingSnapshotCapturedAt()
    {
        var lastSuccessAt = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var lastFailureAt = new DateTimeOffset(2026, 1, 1, 8, 5, 0, TimeSpan.Zero);
        var registry = new RuntimeChannelRegistry();
        var channel = new RecordingRuntimePlcChannel(
            "PLC-01",
            lastSuccessAt: lastSuccessAt,
            lastFailureAt: lastFailureAt);
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        ChannelRuntimeState publishedChannel = Assert.Single(snapshot.Channels);

        Assert.Equal(snapshot.CapturedAt, channel.LastCapturedAt);
        Assert.Equal(snapshot.CapturedAt, snapshot.Health.CapturedAt);
        Assert.Equal(lastSuccessAt, publishedChannel.LastSuccessAt);
        Assert.Equal(lastFailureAt, publishedChannel.LastFailureAt);
        Assert.NotEqual(snapshot.CapturedAt, publishedChannel.LastSuccessAt);
        Assert.NotEqual(snapshot.CapturedAt, publishedChannel.LastFailureAt);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReturnsCurrentSnapshotWithoutRefreshingRegistryChannels()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new RecordingRuntimePlcChannel("PLC-01");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot startedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        RuntimeSnapshot secondSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, channel.GetStateCallCount);
        Assert.Same(startedSnapshot, secondSnapshot);
    }

    [Fact]
    public async Task RefreshSnapshotAsync_PublishesRegistryChannelStatesAndReturnsPublishedSnapshot()
    {
        var registry = new RuntimeChannelRegistry();
        var lastSuccessAt = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var lastFailureAt = new DateTimeOffset(2026, 1, 1, 9, 5, 0, TimeSpan.Zero);
        var channel = new RecordingRuntimePlcChannel(
            "PLC-01",
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning,
            lastSuccessAt: lastSuccessAt,
            lastFailureAt: lastFailureAt);
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        RuntimeSnapshotChangedEventArgs? change = null;
        var runtimeEvents = 0;
        supervisor.SnapshotChanged += (_, args) => change = args;
        supervisor.RuntimeEventRaised += (_, _) => runtimeEvents++;

        RuntimeSnapshot returnedSnapshot = await supervisor.RefreshSnapshotAsync(CancellationToken.None);
        RuntimeSnapshot cachedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.NotNull(change);
        Assert.Equal(0, runtimeEvents);
        Assert.Same(returnedSnapshot, change.Snapshot);
        Assert.Same(returnedSnapshot, cachedSnapshot);
        Assert.Equal(1, change.Revision);
        Assert.Equal(returnedSnapshot.CapturedAt, returnedSnapshot.Health.CapturedAt);
        Assert.Equal(returnedSnapshot.CapturedAt, change.OccurredAt);
        Assert.Equal(returnedSnapshot.CapturedAt, channel.LastCapturedAt);
        Assert.Equal(1, channel.GetStateCallCount);
        ChannelRuntimeState publishedChannel = Assert.Single(returnedSnapshot.Channels);
        Assert.Equal("PLC-01", publishedChannel.PlcId);
        Assert.Equal(PlcLinkState.Reconnecting, publishedChannel.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, publishedChannel.HealthSeverity);
        Assert.Equal(lastSuccessAt, publishedChannel.LastSuccessAt);
        Assert.Equal(lastFailureAt, publishedChannel.LastFailureAt);
        Assert.NotEqual(returnedSnapshot.CapturedAt, publishedChannel.LastSuccessAt);
        Assert.NotEqual(returnedSnapshot.CapturedAt, publishedChannel.LastFailureAt);
        Assert.Equal(1, returnedSnapshot.Health.TotalPlcs);
        Assert.Equal(1, returnedSnapshot.Health.ReconnectingCount);
        Assert.Equal(1, returnedSnapshot.Health.WarningCount);
    }

    [Fact]
    public async Task ReplaceState_DoesNotPublishSnapshotUntilRefreshSnapshotAsyncIsCalled()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Healthy);
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var changes = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => changes.Add(args);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot startedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        IWritableRuntimePlcChannel writable = Assert.IsAssignableFrom<IWritableRuntimePlcChannel>(channel);

        writable.ReplaceState(new RuntimePlcChannelState(
            PlcId: "PLC-01",
            PlcName: "Cutting PLC",
            LineName: "Line-A",
            IsEnabled: true,
            IpAddress: "192.168.0.10",
            Port: 2004,
            LinkState: PlcLinkState.Reconnecting,
            HealthSeverity: PlcHealthSeverity.Warning,
            PollingState: PlcPollingState.Delayed,
            SequenceState: RuntimeSequenceState.Waiting,
            ConfiguredPollingIntervalMs: 500,
            EffectivePollingIntervalMs: 750,
            LastResponseMs: 42,
            ConsecutiveFailures: 3,
            ReconnectCount: 1,
            SuccessRate: 0.9,
            LastSuccessAt: new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero),
            LastFailureAt: new DateTimeOffset(2026, 5, 15, 8, 5, 0, TimeSpan.Zero),
            LastError: "Timeout"));
        RuntimeSnapshot cachedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Single(changes);
        Assert.Same(startedSnapshot, cachedSnapshot);
        ChannelRuntimeState cachedChannel = Assert.Single(cachedSnapshot.Channels);
        Assert.Equal(PlcLinkState.Online, cachedChannel.LinkState);
        Assert.Equal(PlcHealthSeverity.Healthy, cachedChannel.HealthSeverity);

        RuntimeSnapshot refreshedSnapshot = await supervisor.RefreshSnapshotAsync(CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Same(refreshedSnapshot, changes[1].Snapshot);
        ChannelRuntimeState refreshedChannel = Assert.Single(refreshedSnapshot.Channels);
        Assert.Equal(PlcLinkState.Reconnecting, refreshedChannel.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, refreshedChannel.HealthSeverity);
        Assert.Equal(PlcPollingState.Delayed, refreshedChannel.PollingState);
        Assert.Equal(RuntimeSequenceState.Waiting, refreshedChannel.SequenceState);
    }

    [Fact]
    public async Task RefreshSnapshotAsync_IncrementsRevisionAfterStartPublish()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new RecordingRuntimePlcChannel("PLC-01"));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var changes = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => changes.Add(args);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot returnedSnapshot = await supervisor.RefreshSnapshotAsync(CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Equal(1, changes[0].Revision);
        Assert.Equal(2, changes[1].Revision);
        Assert.Same(returnedSnapshot, changes[1].Snapshot);
        Assert.NotSame(changes[0].Snapshot, changes[1].Snapshot);
    }

    [Fact]
    public async Task RefreshSnapshotAsync_WhenRegistryStateReadFailsKeepsExistingSnapshotAndPropagates()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new RecordingRuntimePlcChannel("PLC-01"));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var changes = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => changes.Add(args);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot startedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        registry.Add(new ThrowingRuntimePlcChannel("PLC-02"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => supervisor.RefreshSnapshotAsync(CancellationToken.None));
        RuntimeSnapshot cachedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Single(changes);
        Assert.Equal(1, changes[0].Revision);
        Assert.Same(startedSnapshot, cachedSnapshot);
    }

    [Fact]
    public async Task GetSnapshotAsync_AfterRefreshReturnsCurrentCacheWithoutRefreshingRegistryChannels()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new RecordingRuntimePlcChannel("PLC-01");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);

        RuntimeSnapshot refreshedSnapshot = await supervisor.RefreshSnapshotAsync(CancellationToken.None);
        RuntimeSnapshot firstCachedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        RuntimeSnapshot secondCachedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, channel.GetStateCallCount);
        Assert.Same(refreshedSnapshot, firstCachedSnapshot);
        Assert.Same(refreshedSnapshot, secondCachedSnapshot);
    }

    [Fact]
    public async Task StopAsync_KeepsLastSnapshotWithoutRefreshingRegistryOrPublishingSnapshotChanged()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new RecordingRuntimePlcChannel("PLC-01");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var snapshotChanges = 0;
        supervisor.SnapshotChanged += (_, _) => snapshotChanges++;

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot startedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        await supervisor.StopAsync(CancellationToken.None);
        RuntimeSnapshot stoppedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, snapshotChanges);
        Assert.Equal(1, channel.GetStateCallCount);
        Assert.Same(startedSnapshot, stoppedSnapshot);
    }

    private sealed class RecordingRuntimePlcChannel : IRuntimePlcChannel
    {
        private readonly PlcLinkState _linkState;
        private readonly PlcHealthSeverity _healthSeverity;
        private readonly DateTimeOffset? _lastSuccessAt;
        private readonly DateTimeOffset? _lastFailureAt;

        public RecordingRuntimePlcChannel(
            string plcId,
            PlcLinkState linkState = PlcLinkState.Online,
            PlcHealthSeverity healthSeverity = PlcHealthSeverity.Healthy,
            DateTimeOffset? lastSuccessAt = null,
            DateTimeOffset? lastFailureAt = null)
        {
            PlcId = plcId;
            _linkState = linkState;
            _healthSeverity = healthSeverity;
            _lastSuccessAt = lastSuccessAt;
            _lastFailureAt = lastFailureAt;
        }

        public string PlcId { get; }

        public DateTimeOffset? LastCapturedAt { get; private set; }

        public int GetStateCallCount { get; private set; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
        {
            LastCapturedAt = capturedAt;
            GetStateCallCount++;

            return new ChannelRuntimeState(
                PlcId: PlcId,
                PlcName: $"{PlcId} name",
                LineName: "Line-A",
                IsEnabled: true,
                IpAddress: "127.0.0.1",
                Port: 2004,
                LinkState: _linkState,
                HealthSeverity: _healthSeverity,
                PollingState: PlcPollingState.Polling,
                SequenceState: RuntimeSequenceState.Idle,
                ConfiguredPollingIntervalMs: 500,
                EffectivePollingIntervalMs: 500,
                LastResponseMs: 0,
                ConsecutiveFailures: 0,
                ReconnectCount: 0,
                SuccessRate: 1.0,
                LastSuccessAt: _lastSuccessAt,
                LastFailureAt: _lastFailureAt,
                LastError: null);
        }
    }

    private sealed class ThrowingRuntimePlcChannel : IRuntimePlcChannel
    {
        public ThrowingRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => throw new InvalidOperationException($"Unable to read state for {PlcId}.");
    }
}

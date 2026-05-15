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
    public async Task StartAsync_PassesSnapshotCapturedAtToRegistryChannels()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new RecordingRuntimePlcChannel("PLC-01");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);

        await supervisor.StartAsync(CancellationToken.None);
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(snapshot.CapturedAt, channel.LastCapturedAt);
        Assert.Equal(snapshot.CapturedAt, Assert.Single(snapshot.Channels).LastSuccessAt);
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
        public RecordingRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
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
                LinkState: PlcLinkState.Online,
                HealthSeverity: PlcHealthSeverity.Healthy,
                PollingState: PlcPollingState.Polling,
                SequenceState: RuntimeSequenceState.Idle,
                ConfiguredPollingIntervalMs: 500,
                EffectivePollingIntervalMs: 500,
                LastResponseMs: 0,
                ConsecutiveFailures: 0,
                ReconnectCount: 0,
                SuccessRate: 1.0,
                LastSuccessAt: capturedAt,
                LastFailureAt: null,
                LastError: null);
        }
    }
}

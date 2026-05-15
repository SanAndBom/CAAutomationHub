using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingResultRuntimeSnapshotEndToEndTests
{
    [Fact]
    public async Task PublishSuccess_UpdatesRuntimeSnapshotWithOccurredAt()
    {
        const string plcId = "PLC-1";
        var occurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        const int responseTimeMs = 25;
        var previousLastSuccessAt = DateTimeOffset.Parse("2025-12-31T23:59:00Z");
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel(
            plcId: plcId,
            plcName: "Cutting PLC",
            linkState: PlcLinkState.Offline,
            healthSeverity: PlcHealthSeverity.Warning,
            pollingState: PlcPollingState.Delayed,
            lastResponseMs: 10,
            consecutiveFailures: 2,
            lastSuccessAt: previousLastSuccessAt,
            lastError: "previous failure");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationResult result = await orchestrator.PublishAsync(
            ChannelPollingResult.Success(plcId, occurredAt, responseTimeMs),
            CancellationToken.None);

        RuntimePlcChannelState runtimeState = channel.GetRuntimeState();
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        ChannelRuntimeState snapshotChannel = FindChannel(snapshot, plcId);
        RuntimeSnapshotChangedEventArgs change = Assert.Single(snapshotChanges);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.PublishResult);
        Assert.True(result.PublishResult.PublishSucceeded);
        Assert.Equal(1, result.PublishResult.UpdatedCount);
        Assert.Equal(occurredAt, runtimeState.LastSuccessAt);
        Assert.Equal(0, runtimeState.ConsecutiveFailures);
        Assert.Null(runtimeState.LastError);
        Assert.Equal(responseTimeMs, runtimeState.LastResponseMs);
        Assert.Equal(occurredAt, snapshotChannel.LastSuccessAt);
        Assert.Equal(0, snapshotChannel.ConsecutiveFailures);
        Assert.Equal(responseTimeMs, snapshotChannel.LastResponseMs);
        Assert.Equal(PlcLinkState.Online, snapshotChannel.LinkState);
        Assert.Equal(PlcHealthSeverity.Healthy, snapshotChannel.HealthSeverity);
        Assert.Equal(PlcPollingState.Polling, snapshotChannel.PollingState);
        Assert.NotEqual(occurredAt, snapshot.CapturedAt);
        Assert.Equal(snapshot.CapturedAt, snapshot.Health.CapturedAt);
        Assert.Same(snapshot, change.Snapshot);
        Assert.Equal(snapshot.CapturedAt, change.OccurredAt);
    }

    [Fact]
    public async Task PublishFailure_UpdatesRuntimeSnapshotWithFailureOccurredAt()
    {
        const string plcId = "PLC-1";
        var previousLastSuccessAt = DateTimeOffset.Parse("2025-12-31T23:59:00Z");
        var occurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        const int previousLastResponseMs = 30;
        const string errorMessage = "polling failed";
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel(
            plcId: plcId,
            plcName: "Cutting PLC",
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Healthy,
            pollingState: PlcPollingState.Polling,
            lastResponseMs: previousLastResponseMs,
            consecutiveFailures: 2,
            lastSuccessAt: previousLastSuccessAt);
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationResult result = await orchestrator.PublishAsync(
            ChannelPollingResult.Failure(
                plcId,
                occurredAt,
                ChannelPollingFailureKind.Timeout,
                errorMessage,
                responseTimeMs: null),
            CancellationToken.None);

        RuntimePlcChannelState runtimeState = channel.GetRuntimeState();
        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        ChannelRuntimeState snapshotChannel = FindChannel(snapshot, plcId);
        RuntimeSnapshotChangedEventArgs change = Assert.Single(snapshotChanges);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.PublishResult);
        Assert.True(result.PublishResult.PublishSucceeded);
        Assert.Equal(1, result.PublishResult.UpdatedCount);
        Assert.Equal(occurredAt, runtimeState.LastFailureAt);
        Assert.Equal(previousLastSuccessAt, runtimeState.LastSuccessAt);
        Assert.Equal(3, runtimeState.ConsecutiveFailures);
        Assert.Equal(previousLastResponseMs, runtimeState.LastResponseMs);
        Assert.Equal(errorMessage, runtimeState.LastError);
        Assert.Equal(occurredAt, snapshotChannel.LastFailureAt);
        Assert.Equal(previousLastSuccessAt, snapshotChannel.LastSuccessAt);
        Assert.Equal(3, snapshotChannel.ConsecutiveFailures);
        Assert.Equal(previousLastResponseMs, snapshotChannel.LastResponseMs);
        Assert.Equal(errorMessage, snapshotChannel.LastError);
        Assert.Equal(PlcHealthSeverity.Warning, snapshotChannel.HealthSeverity);
        Assert.Equal(PlcPollingState.Delayed, snapshotChannel.PollingState);
        Assert.Equal(PlcLinkState.Online, snapshotChannel.LinkState);
        Assert.NotEqual(occurredAt, snapshot.CapturedAt);
        Assert.Equal(snapshot.CapturedAt, snapshot.Health.CapturedAt);
        Assert.Same(snapshot, change.Snapshot);
        Assert.Equal(snapshot.CapturedAt, change.OccurredAt);
    }

    private static ChannelRuntimeState FindChannel(RuntimeSnapshot snapshot, string plcId)
        => Assert.Single(snapshot.Channels, channel => channel.PlcId == plcId);
}

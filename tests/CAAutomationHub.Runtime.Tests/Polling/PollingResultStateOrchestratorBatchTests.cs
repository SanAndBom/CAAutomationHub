using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingResultStateOrchestratorBatchTests
{
    [Fact]
    public async Task PublishBatchAsync_ThrowsWhenResultsIsNull()
    {
        var registry = new RuntimeChannelRegistry();
        var coordinator = new PollingPublishCoordinator(registry, _ => Task.FromResult(RuntimeSnapshot.Empty));
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => orchestrator.PublishBatchAsync(null!, CancellationToken.None));

        Assert.Equal("results", exception.ParamName);
    }

    [Fact]
    public async Task PublishBatchAsync_WhenResultsEmpty_DoesNotPublishAndDoesNotRefresh()
    {
        var registry = new RuntimeChannelRegistry();
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationBatchResult result = await orchestrator.PublishBatchAsync(
            [],
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.UpdatesCount);
        Assert.Null(result.PublishResult);
        Assert.Empty(result.ItemResults);
        Assert.Empty(snapshotChanges);
    }

    [Fact]
    public async Task PublishBatchAsync_WithMultipleWritableResults_PublishesSingleBatchAndRefreshesOnce()
    {
        var firstOccurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var secondOccurredAt = DateTimeOffset.Parse("2026-01-01T00:00:10Z");
        var registry = new RuntimeChannelRegistry();
        var first = new InMemoryRuntimePlcChannel(
            plcId: "PLC-1",
            plcName: "Cutting PLC",
            consecutiveFailures: 2,
            lastError: "previous failure");
        var second = new InMemoryRuntimePlcChannel(
            plcId: "PLC-2",
            plcName: "Packaging PLC",
            consecutiveFailures: 3,
            lastError: "previous failure");
        registry.Add(first);
        registry.Add(second);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationBatchResult result = await orchestrator.PublishBatchAsync(
            [
                ChannelPollingResult.Success("PLC-1", firstOccurredAt, responseTimeMs: 21),
                ChannelPollingResult.Success("PLC-2", secondOccurredAt, responseTimeMs: 32),
            ],
            CancellationToken.None);

        RuntimeSnapshot snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        RuntimeSnapshotChangedEventArgs change = Assert.Single(snapshotChanges);
        ChannelRuntimeState firstSnapshot = FindChannel(snapshot, "PLC-1");
        ChannelRuntimeState secondSnapshot = FindChannel(snapshot, "PLC-2");
        Assert.True(result.Succeeded);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.UpdatesCount);
        Assert.NotNull(result.PublishResult);
        Assert.Equal(2, result.PublishResult.UpdatedCount);
        Assert.Equal(firstOccurredAt, first.GetRuntimeState().LastSuccessAt);
        Assert.Equal(secondOccurredAt, second.GetRuntimeState().LastSuccessAt);
        Assert.Equal(firstOccurredAt, firstSnapshot.LastSuccessAt);
        Assert.Equal(secondOccurredAt, secondSnapshot.LastSuccessAt);
        Assert.NotEqual(firstOccurredAt, snapshot.CapturedAt);
        Assert.NotEqual(secondOccurredAt, snapshot.CapturedAt);
        Assert.Equal(snapshot.CapturedAt, snapshot.Health.CapturedAt);
        Assert.Equal(snapshot.CapturedAt, change.OccurredAt);
    }

    [Fact]
    public async Task PublishBatchAsync_WithMissingAndWritableResults_AllowsPartialSuccess()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-1", "Cutting PLC");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationBatchResult result = await orchestrator.PublishBatchAsync(
            [
                ChannelPollingResult.Success("PLC-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
                ChannelPollingResult.Success("PLC-MISSING", DateTimeOffset.Parse("2026-01-01T00:00:10Z")),
            ],
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(1, result.UpdatesCount);
        Assert.NotNull(result.PublishResult);
        Assert.True(result.PublishResult.PublishSucceeded);
        Assert.Equal(1, result.PublishResult.UpdatedCount);
        Assert.Contains(
            result.ItemResults,
            item => item.PlcId == "PLC-MISSING"
                && !item.Succeeded
                && item.ErrorMessage is not null
                && item.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase));
        Assert.Single(snapshotChanges);
    }

    [Fact]
    public async Task PublishBatchAsync_WhenNoUpdatesCreated_DoesNotCallCoordinatorRefresh()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new ReadOnlyRuntimePlcChannel("PLC-READONLY"));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationBatchResult result = await orchestrator.PublishBatchAsync(
            [
                ChannelPollingResult.Success("PLC-MISSING", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
                ChannelPollingResult.Success("PLC-READONLY", DateTimeOffset.Parse("2026-01-01T00:00:10Z")),
            ],
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(0, result.SucceededCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.UpdatesCount);
        Assert.Null(result.PublishResult);
        Assert.Empty(snapshotChanges);
    }

    [Fact]
    public async Task PublishBatchAsync_WithDuplicatePlcId_SkipsDuplicateWithoutPublishingSecondUpdate()
    {
        var firstOccurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var duplicateOccurredAt = DateTimeOffset.Parse("2026-01-01T00:00:10Z");
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-1", "Cutting PLC");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);

        PollingResultStateOrchestrationBatchResult result = await orchestrator.PublishBatchAsync(
            [
                ChannelPollingResult.Success("PLC-1", firstOccurredAt, responseTimeMs: 21),
                ChannelPollingResult.Success("PLC-1", duplicateOccurredAt, responseTimeMs: 99),
            ],
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.UpdatesCount);
        Assert.NotNull(result.PublishResult);
        Assert.Equal(1, result.PublishResult.UpdatedCount);
        Assert.Equal(firstOccurredAt, channel.GetRuntimeState().LastSuccessAt);
        Assert.Equal(21, channel.GetRuntimeState().LastResponseMs);
        Assert.Contains(
            result.ItemResults,
            item => item.PlcId == "PLC-1"
                && !item.Succeeded
                && item.ErrorMessage is not null
                && item.ErrorMessage.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PublishAsync_SingleResult_UsesBatchPathWithoutBreakingExistingBehavior()
    {
        var occurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-1", "Cutting PLC");
        registry.Add(channel);
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var coordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var snapshotChanges = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => snapshotChanges.Add(args);

        PollingResultStateOrchestrationResult result = await orchestrator.PublishAsync(
            ChannelPollingResult.Success("PLC-1", occurredAt, responseTimeMs: 18),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.PublishResult);
        Assert.True(result.PublishResult.PublishSucceeded);
        Assert.Equal(1, result.PublishResult.UpdatedCount);
        Assert.Equal(occurredAt, channel.GetRuntimeState().LastSuccessAt);
        Assert.Single(snapshotChanges);
    }

    private static ChannelRuntimeState FindChannel(RuntimeSnapshot snapshot, string plcId)
        => Assert.Single(snapshot.Channels, channel => channel.PlcId == plcId);

    private sealed class ReadOnlyRuntimePlcChannel : IRuntimePlcChannel
    {
        public ReadOnlyRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => new(
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
                LastSuccessAt: null,
                LastFailureAt: null,
                LastError: null);
    }
}

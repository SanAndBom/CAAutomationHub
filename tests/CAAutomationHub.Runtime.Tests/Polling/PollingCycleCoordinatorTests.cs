using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingCycleCoordinatorTests
{
    [Fact]
    public async Task PublishCycleAsync_WhenNoCycleInFlight_PublishesBatch()
    {
        var occurredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-1", "Cutting PLC"));
        registry.Add(new InMemoryRuntimePlcChannel("PLC-2", "Packaging PLC"));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var publishCoordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, publishCoordinator);
        var coordinator = new PollingCycleCoordinator(orchestrator);

        PollingCyclePublishResult result = await coordinator.PublishCycleAsync(
            [
                ChannelPollingResult.Success("PLC-1", occurredAt, responseTimeMs: 21),
                ChannelPollingResult.Success("PLC-2", occurredAt.AddSeconds(10), responseTimeMs: 32),
            ],
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Skipped);
        Assert.False(result.Cancelled);
        Assert.Null(result.SkipReason);
        Assert.Equal(2, result.RequestedCount);
        Assert.NotNull(result.BatchResult);
        Assert.Equal(2, result.BatchResult.TotalCount);
        Assert.Equal(2, result.BatchResult.UpdatesCount);
        Assert.NotEqual(default, result.CycleStartedAt);
        Assert.NotNull(result.CycleCompletedAt);
        Assert.True(result.CycleCompletedAt >= result.CycleStartedAt);
    }

    [Fact]
    public async Task PublishCycleAsync_WhenCycleAlreadyInFlight_SkipsSecondCycle()
    {
        PollingResultStateOrchestrationBatchResult batchResult = await CreateSuccessfulBatchResultAsync();
        using var publisher = new ControllableBatchPublisher(batchResult);
        var coordinator = new PollingCycleCoordinator(publisher);
        IReadOnlyCollection<ChannelPollingResult> results =
        [
            ChannelPollingResult.Success("PLC-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
        ];

        Task<PollingCyclePublishResult> first = coordinator.PublishCycleAsync(results, CancellationToken.None);
        await publisher.WaitUntilStartedAsync();

        PollingCyclePublishResult second = await coordinator.PublishCycleAsync(results, CancellationToken.None);
        publisher.Complete();
        PollingCyclePublishResult firstResult = await first;

        Assert.True(firstResult.Succeeded);
        Assert.False(firstResult.Skipped);
        Assert.False(second.Succeeded);
        Assert.True(second.Skipped);
        Assert.False(second.Cancelled);
        Assert.NotNull(second.SkipReason);
        Assert.Contains("progress", second.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(second.BatchResult);
        Assert.Equal(1, publisher.PublishCallCount);
    }

    [Fact]
    public async Task PublishCycleAsync_WhenCancellationRequestedBeforeStart_DiscardsWithoutPublishing()
    {
        PollingResultStateOrchestrationBatchResult batchResult = await CreateSuccessfulBatchResultAsync();
        using var publisher = new ControllableBatchPublisher(batchResult);
        var coordinator = new PollingCycleCoordinator(publisher);
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        PollingCyclePublishResult result = await coordinator.PublishCycleAsync(
            [
                ChannelPollingResult.Success("PLC-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
            ],
            cancellationSource.Token);

        Assert.False(result.Succeeded);
        Assert.False(result.Skipped);
        Assert.True(result.Cancelled);
        Assert.NotNull(result.SkipReason);
        Assert.Contains("cancel", result.SkipReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, result.RequestedCount);
        Assert.Null(result.BatchResult);
        Assert.Equal(0, publisher.PublishCallCount);
        Assert.Null(result.CycleCompletedAt);
    }

    [Fact]
    public async Task PublishCycleAsync_AfterPublishStarted_CompletesGracefully()
    {
        PollingResultStateOrchestrationBatchResult batchResult = await CreateSuccessfulBatchResultAsync();
        using var publisher = new ControllableBatchPublisher(batchResult);
        var coordinator = new PollingCycleCoordinator(publisher);
        using var cancellationSource = new CancellationTokenSource();

        Task<PollingCyclePublishResult> publish = coordinator.PublishCycleAsync(
            [
                ChannelPollingResult.Success("PLC-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
            ],
            cancellationSource.Token);
        await publisher.WaitUntilStartedAsync();
        await cancellationSource.CancelAsync();
        publisher.Complete();

        PollingCyclePublishResult result = await publish;

        Assert.True(result.Succeeded);
        Assert.False(result.Cancelled);
        Assert.NotNull(result.BatchResult);
        Assert.Single(publisher.CapturedTokens, token => !token.CanBeCanceled);
    }

    private static async Task<PollingResultStateOrchestrationBatchResult> CreateSuccessfulBatchResultAsync()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-1", "Cutting PLC"));
        var supervisor = new InMemoryAutomationHubSupervisor(registry);
        var publishCoordinator = new PollingPublishCoordinator(registry, supervisor.RefreshSnapshotAsync);
        var orchestrator = new PollingResultStateOrchestrator(registry, publishCoordinator);

        return await orchestrator.PublishBatchAsync(
            [
                ChannelPollingResult.Success("PLC-1", DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
            ],
            CancellationToken.None);
    }

    private sealed class ControllableBatchPublisher : IPollingResultBatchPublisher, IDisposable
    {
        private readonly PollingResultStateOrchestrationBatchResult _result;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _complete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ControllableBatchPublisher(PollingResultStateOrchestrationBatchResult result)
        {
            _result = result;
        }

        public int PublishCallCount { get; private set; }

        public List<CancellationToken> CapturedTokens { get; } = [];

        public async Task<PollingResultStateOrchestrationBatchResult> PublishBatchAsync(
            IReadOnlyCollection<ChannelPollingResult> results,
            CancellationToken cancellationToken = default)
        {
            PublishCallCount++;
            CapturedTokens.Add(cancellationToken);
            _started.TrySetResult();
            await _complete.Task.ConfigureAwait(false);
            return _result;
        }

        public Task WaitUntilStartedAsync()
            => _started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public void Complete()
            => _complete.TrySetResult();

        public void Dispose()
        {
            _started.TrySetCanceled();
            _complete.TrySetCanceled();
        }
    }
}

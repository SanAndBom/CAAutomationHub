namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingCycleCoordinator
{
    private readonly IPollingResultBatchPublisher _publisher;
    private int _publishInFlight;

    public PollingCycleCoordinator(PollingResultStateOrchestrator orchestrator)
        : this((IPollingResultBatchPublisher)(orchestrator ?? throw new ArgumentNullException(nameof(orchestrator))))
    {
    }

    internal PollingCycleCoordinator(IPollingResultBatchPublisher publisher)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    public async Task<PollingCyclePublishResult> PublishCycleAsync(
        IReadOnlyCollection<ChannelPollingResult> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        DateTimeOffset cycleStartedAt = DateTimeOffset.UtcNow;
        int requestedCount = results.Count;

        if (cancellationToken.IsCancellationRequested)
        {
            return PollingCyclePublishResult.CancelledBeforeStart(
                cycleStartedAt,
                requestedCount,
                "Cycle publish was cancelled before start.");
        }

        if (Interlocked.CompareExchange(ref _publishInFlight, 1, 0) != 0)
        {
            return PollingCyclePublishResult.SkippedCycle(
                cycleStartedAt,
                requestedCount,
                "Cycle already in progress.");
        }

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return PollingCyclePublishResult.CancelledBeforeStart(
                    cycleStartedAt,
                    requestedCount,
                    "Cycle publish was cancelled before start.");
            }

            // Once a cycle enters the Runtime publish path, complete the batch
            // and snapshot refresh gracefully. Force cancellation is a later
            // boundary decision, not part of this manual-cycle skeleton.
            PollingResultStateOrchestrationBatchResult batchResult = await _publisher.PublishBatchAsync(
                results,
                CancellationToken.None).ConfigureAwait(false);

            return PollingCyclePublishResult.Published(
                cycleStartedAt,
                DateTimeOffset.UtcNow,
                requestedCount,
                batchResult);
        }
        finally
        {
            Volatile.Write(ref _publishInFlight, 0);
        }
    }
}

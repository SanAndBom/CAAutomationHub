namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingCyclePublishResult
{
    private PollingCyclePublishResult(
        bool succeeded,
        bool skipped,
        bool cancelled,
        string? skipReason,
        DateTimeOffset cycleStartedAt,
        DateTimeOffset? cycleCompletedAt,
        int requestedCount,
        PollingResultStateOrchestrationBatchResult? batchResult)
    {
        Succeeded = succeeded;
        Skipped = skipped;
        Cancelled = cancelled;
        SkipReason = skipReason;
        CycleStartedAt = cycleStartedAt;
        CycleCompletedAt = cycleCompletedAt;
        RequestedCount = requestedCount;
        BatchResult = batchResult;
    }

    public bool Succeeded { get; }

    public bool Skipped { get; }

    public bool Cancelled { get; }

    public string? SkipReason { get; }

    public DateTimeOffset CycleStartedAt { get; }

    public DateTimeOffset? CycleCompletedAt { get; }

    public int RequestedCount { get; }

    public PollingResultStateOrchestrationBatchResult? BatchResult { get; }

    internal static PollingCyclePublishResult Published(
        DateTimeOffset cycleStartedAt,
        DateTimeOffset cycleCompletedAt,
        int requestedCount,
        PollingResultStateOrchestrationBatchResult batchResult)
        => new(
            succeeded: batchResult?.Succeeded == true,
            skipped: false,
            cancelled: false,
            skipReason: null,
            cycleStartedAt,
            cycleCompletedAt,
            requestedCount,
            batchResult ?? throw new ArgumentNullException(nameof(batchResult)));

    internal static PollingCyclePublishResult SkippedCycle(
        DateTimeOffset cycleStartedAt,
        int requestedCount,
        string skipReason)
        => new(
            succeeded: false,
            skipped: true,
            cancelled: false,
            skipReason: skipReason ?? throw new ArgumentNullException(nameof(skipReason)),
            cycleStartedAt,
            cycleCompletedAt: null,
            requestedCount,
            batchResult: null);

    internal static PollingCyclePublishResult CancelledBeforeStart(
        DateTimeOffset cycleStartedAt,
        int requestedCount,
        string skipReason)
        => new(
            succeeded: false,
            skipped: false,
            cancelled: true,
            skipReason: skipReason ?? throw new ArgumentNullException(nameof(skipReason)),
            cycleStartedAt,
            cycleCompletedAt: null,
            requestedCount,
            batchResult: null);
}

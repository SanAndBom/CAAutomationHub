namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingPublishResult
{
    private PollingPublishResult(
        int requestedCount,
        int updatedCount,
        IReadOnlyList<string> missingChannelIds,
        IReadOnlyList<string> nonWritableChannelIds,
        IReadOnlyList<PollingPublishUpdateFailure> updateFailures,
        bool publishAttempted,
        bool publishSucceeded,
        Exception? publishException)
    {
        RequestedCount = requestedCount;
        UpdatedCount = updatedCount;
        MissingChannelIds = missingChannelIds;
        NonWritableChannelIds = nonWritableChannelIds;
        UpdateFailures = updateFailures;
        PublishAttempted = publishAttempted;
        PublishSucceeded = publishSucceeded;
        PublishException = publishException;
    }

    public int RequestedCount { get; }

    public int UpdatedCount { get; }

    public IReadOnlyList<string> MissingChannelIds { get; }

    public IReadOnlyList<string> NonWritableChannelIds { get; }

    public IReadOnlyList<PollingPublishUpdateFailure> UpdateFailures { get; }

    public bool PublishAttempted { get; }

    public bool PublishSucceeded { get; }

    public Exception? PublishException { get; }

    internal static PollingPublishResult NotAttempted(
        int requestedCount,
        int updatedCount,
        IReadOnlyList<string> missingChannelIds,
        IReadOnlyList<string> nonWritableChannelIds,
        IReadOnlyList<PollingPublishUpdateFailure> updateFailures)
        => new(
            requestedCount,
            updatedCount,
            missingChannelIds.ToArray(),
            nonWritableChannelIds.ToArray(),
            updateFailures.ToArray(),
            publishAttempted: false,
            publishSucceeded: false,
            publishException: null);

    internal static PollingPublishResult Succeeded(
        int requestedCount,
        int updatedCount,
        IReadOnlyList<string> missingChannelIds,
        IReadOnlyList<string> nonWritableChannelIds,
        IReadOnlyList<PollingPublishUpdateFailure> updateFailures)
        => new(
            requestedCount,
            updatedCount,
            missingChannelIds.ToArray(),
            nonWritableChannelIds.ToArray(),
            updateFailures.ToArray(),
            publishAttempted: true,
            publishSucceeded: true,
            publishException: null);

    internal static PollingPublishResult Failed(
        int requestedCount,
        int updatedCount,
        IReadOnlyList<string> missingChannelIds,
        IReadOnlyList<string> nonWritableChannelIds,
        IReadOnlyList<PollingPublishUpdateFailure> updateFailures,
        Exception publishException)
        => new(
            requestedCount,
            updatedCount,
            missingChannelIds.ToArray(),
            nonWritableChannelIds.ToArray(),
            updateFailures.ToArray(),
            publishAttempted: true,
            publishSucceeded: false,
            publishException: publishException);
}

public sealed class PollingPublishUpdateFailure
{
    public PollingPublishUpdateFailure(string plcId, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        PlcId = plcId;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    public string PlcId { get; }

    public Exception Exception { get; }
}

using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingResultStateOrchestrator
{
    private readonly RuntimeChannelRegistry _channelRegistry;
    private readonly PollingPublishCoordinator _publishCoordinator;

    public PollingResultStateOrchestrator(
        RuntimeChannelRegistry channelRegistry,
        PollingPublishCoordinator publishCoordinator)
    {
        _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
        _publishCoordinator = publishCoordinator ?? throw new ArgumentNullException(nameof(publishCoordinator));
    }

    public async Task<PollingResultStateOrchestrationResult> PublishAsync(
        ChannelPollingResult result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(result);

        PollingResultStateOrchestrationBatchResult batchResult = await PublishBatchAsync(
            [result],
            cancellationToken).ConfigureAwait(false);

        return batchResult.ItemResults.Single();
    }

    public async Task<PollingResultStateOrchestrationBatchResult> PublishBatchAsync(
        IReadOnlyCollection<ChannelPollingResult> results,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
        {
            return PollingResultStateOrchestrationBatchResult.Create(
                totalCount: 0,
                updatesCount: 0,
                publishResult: null,
                itemResults: []);
        }

        var seenPlcIds = new HashSet<string>(StringComparer.Ordinal);
        var updates = new List<PollingChannelUpdate>();
        var updateItemIndexes = new List<int>();
        var itemResults = new List<PollingResultStateOrchestrationResult?>(results.Count);

        foreach (ChannelPollingResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (result is null)
            {
                throw new ArgumentException("Polling results must not contain null items.", nameof(results));
            }

            if (!seenPlcIds.Add(result.PlcId))
            {
                itemResults.Add(PollingResultStateOrchestrationResult.SkippedResult(
                    result.PlcId,
                    $"Duplicate polling result for PLC id '{result.PlcId}' was skipped."));
                continue;
            }

            if (!TryCreateUpdate(result, out PollingChannelUpdate? update, out PollingResultStateOrchestrationResult? failure))
            {
                itemResults.Add(failure);
                continue;
            }

            updates.Add(update!);
            updateItemIndexes.Add(itemResults.Count);
            itemResults.Add(null);
        }

        if (updates.Count == 0)
        {
            return PollingResultStateOrchestrationBatchResult.Create(
                totalCount: results.Count,
                updatesCount: 0,
                publishResult: null,
                itemResults: CompleteItemResults(itemResults));
        }

        PollingPublishResult publishResult = await _publishCoordinator.PublishAsync(
            updates,
            cancellationToken).ConfigureAwait(false);

        ApplyPublishResult(itemResults, updateItemIndexes, updates, publishResult);

        return PollingResultStateOrchestrationBatchResult.Create(
            totalCount: results.Count,
            updatesCount: updates.Count,
            publishResult: publishResult,
            itemResults: CompleteItemResults(itemResults));
    }

    private bool TryCreateUpdate(
        ChannelPollingResult result,
        out PollingChannelUpdate? update,
        out PollingResultStateOrchestrationResult? failure)
    {
        if (!_channelRegistry.TryGetChannel(result.PlcId, out IRuntimePlcChannel? channel))
        {
            update = null;
            failure = PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                $"Runtime PLC channel '{result.PlcId}' was not found.");
            return false;
        }

        if (channel is not IWritableRuntimePlcChannel writable)
        {
            update = null;
            failure = PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                $"Runtime PLC channel '{result.PlcId}' is not writable.");
            return false;
        }

        try
        {
            RuntimePlcChannelState previous = writable.GetRuntimeState();
            RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);
            update = new PollingChannelUpdate(result.PlcId, next);
            failure = null;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            update = null;
            failure = PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                exception.Message);
            return false;
        }
    }

    private static void ApplyPublishResult(
        List<PollingResultStateOrchestrationResult?> itemResults,
        IReadOnlyList<int> updateItemIndexes,
        IReadOnlyList<PollingChannelUpdate> updates,
        PollingPublishResult publishResult)
    {
        var missingChannelIds = publishResult.MissingChannelIds.ToHashSet(StringComparer.Ordinal);
        var nonWritableChannelIds = publishResult.NonWritableChannelIds.ToHashSet(StringComparer.Ordinal);
        var failedUpdateIds = publishResult.UpdateFailures
            .Select(failure => failure.PlcId)
            .ToHashSet(StringComparer.Ordinal);

        for (var index = 0; index < updates.Count; index++)
        {
            PollingChannelUpdate update = updates[index];
            int itemIndex = updateItemIndexes[index];

            if (publishResult.PublishSucceeded
                && !missingChannelIds.Contains(update.PlcId)
                && !nonWritableChannelIds.Contains(update.PlcId)
                && !failedUpdateIds.Contains(update.PlcId))
            {
                itemResults[itemIndex] = PollingResultStateOrchestrationResult.Success(
                    update.PlcId,
                    publishResult);
                continue;
            }

            itemResults[itemIndex] = PollingResultStateOrchestrationResult.Failure(
                update.PlcId,
                CreatePublishFailureMessage(update.PlcId, publishResult),
                publishResult);
        }
    }

    private static string CreatePublishFailureMessage(
        string plcId,
        PollingPublishResult publishResult)
    {
        PollingPublishUpdateFailure? updateFailure = publishResult.UpdateFailures
            .FirstOrDefault(failure => string.Equals(failure.PlcId, plcId, StringComparison.Ordinal));

        if (updateFailure is not null)
        {
            return updateFailure.Exception.Message;
        }

        if (publishResult.MissingChannelIds.Contains(plcId, StringComparer.Ordinal))
        {
            return $"Runtime PLC channel '{plcId}' was not found during polling publish.";
        }

        if (publishResult.NonWritableChannelIds.Contains(plcId, StringComparer.Ordinal))
        {
            return $"Runtime PLC channel '{plcId}' was not writable during polling publish.";
        }

        return publishResult.PublishException?.Message ?? "Polling publish did not succeed.";
    }

    private static IReadOnlyList<PollingResultStateOrchestrationResult> CompleteItemResults(
        IReadOnlyList<PollingResultStateOrchestrationResult?> itemResults)
        => itemResults
            .Select(item => item ?? throw new InvalidOperationException("Polling item result was not completed."))
            .ToArray();
}

public sealed class PollingResultStateOrchestrationBatchResult
{
    private PollingResultStateOrchestrationBatchResult(
        int totalCount,
        int updatesCount,
        PollingPublishResult? publishResult,
        IReadOnlyList<PollingResultStateOrchestrationResult> itemResults)
    {
        TotalCount = totalCount;
        UpdatesCount = updatesCount;
        PublishResult = publishResult;
        ItemResults = itemResults;
        SucceededCount = itemResults.Count(item => item.Succeeded);
        FailedCount = itemResults.Count(item => !item.Succeeded && !item.Skipped);
        SkippedCount = itemResults.Count(item => item.Skipped);
        Succeeded = FailedCount == 0
            && SkippedCount == 0
            && (UpdatesCount == 0 || PublishResult?.PublishSucceeded == true);
    }

    public bool Succeeded { get; }

    public int TotalCount { get; }

    public int SucceededCount { get; }

    public int FailedCount { get; }

    public int SkippedCount { get; }

    public int UpdatesCount { get; }

    public PollingPublishResult? PublishResult { get; }

    public IReadOnlyList<PollingResultStateOrchestrationResult> ItemResults { get; }

    internal static PollingResultStateOrchestrationBatchResult Create(
        int totalCount,
        int updatesCount,
        PollingPublishResult? publishResult,
        IReadOnlyList<PollingResultStateOrchestrationResult> itemResults)
    {
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), totalCount, "Total count must not be negative.");
        }

        if (updatesCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(updatesCount), updatesCount, "Updates count must not be negative.");
        }

        ArgumentNullException.ThrowIfNull(itemResults);

        return new(
            totalCount,
            updatesCount,
            publishResult,
            itemResults.ToArray());
    }
}

public sealed class PollingResultStateOrchestrationResult
{
    private PollingResultStateOrchestrationResult(
        string plcId,
        bool succeeded,
        bool skipped,
        string? errorMessage,
        PollingPublishResult? publishResult)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        PlcId = plcId;
        Succeeded = succeeded;
        Skipped = skipped;
        ErrorMessage = errorMessage;
        PublishResult = publishResult;
    }

    public string PlcId { get; }

    public bool Succeeded { get; }

    public bool Skipped { get; }

    public string? ErrorMessage { get; }

    public PollingPublishResult? PublishResult { get; }

    internal static PollingResultStateOrchestrationResult Success(
        string plcId,
        PollingPublishResult publishResult)
        => new(
            plcId,
            succeeded: true,
            skipped: false,
            errorMessage: null,
            publishResult: publishResult ?? throw new ArgumentNullException(nameof(publishResult)));

    internal static PollingResultStateOrchestrationResult Failure(
        string plcId,
        string errorMessage,
        PollingPublishResult? publishResult = null)
        => new(
            plcId,
            succeeded: false,
            skipped: false,
            errorMessage: errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            publishResult: publishResult);

    internal static PollingResultStateOrchestrationResult SkippedResult(
        string plcId,
        string errorMessage,
        PollingPublishResult? publishResult = null)
        => new(
            plcId,
            succeeded: false,
            skipped: true,
            errorMessage: errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            publishResult: publishResult);
}

using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingPublishCoordinator
{
    private readonly RuntimeChannelRegistry _channelRegistry;
    private readonly Func<CancellationToken, Task<RuntimeSnapshot>> _refreshSnapshotAsync;

    public PollingPublishCoordinator(
        RuntimeChannelRegistry channelRegistry,
        Func<CancellationToken, Task<RuntimeSnapshot>> refreshSnapshotAsync)
    {
        _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
        _refreshSnapshotAsync = refreshSnapshotAsync ?? throw new ArgumentNullException(nameof(refreshSnapshotAsync));
    }

    public async Task<PollingPublishResult> PublishAsync(
        IReadOnlyList<PollingChannelUpdate> updates,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(updates);

        var missingChannelIds = new List<string>();
        var nonWritableChannelIds = new List<string>();
        var updateFailures = new List<PollingPublishUpdateFailure>();
        var updatedCount = 0;

        for (var index = 0; index < updates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PollingChannelUpdate update = updates[index]
                ?? throw new ArgumentException("Polling channel updates must not contain null items.", nameof(updates));

            if (!_channelRegistry.TryGetChannel(update.PlcId, out IRuntimePlcChannel? channel))
            {
                missingChannelIds.Add(update.PlcId);
                continue;
            }

            if (channel is not IWritableRuntimePlcChannel writable)
            {
                nonWritableChannelIds.Add(update.PlcId);
                continue;
            }

            try
            {
                writable.ReplaceState(update.State);
                updatedCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                updateFailures.Add(new PollingPublishUpdateFailure(update.PlcId, exception));
            }
        }

        if (updatedCount == 0)
        {
            return PollingPublishResult.NotAttempted(
                requestedCount: updates.Count,
                updatedCount: updatedCount,
                missingChannelIds: missingChannelIds,
                nonWritableChannelIds: nonWritableChannelIds,
                updateFailures: updateFailures);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = await _refreshSnapshotAsync(cancellationToken).ConfigureAwait(false);

            return PollingPublishResult.Succeeded(
                requestedCount: updates.Count,
                updatedCount: updatedCount,
                missingChannelIds: missingChannelIds,
                nonWritableChannelIds: nonWritableChannelIds,
                updateFailures: updateFailures);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return PollingPublishResult.Failed(
                requestedCount: updates.Count,
                updatedCount: updatedCount,
                missingChannelIds: missingChannelIds,
                nonWritableChannelIds: nonWritableChannelIds,
                updateFailures: updateFailures,
                publishException: exception);
        }
    }
}

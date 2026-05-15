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

        if (!_channelRegistry.TryGetChannel(result.PlcId, out IRuntimePlcChannel? channel))
        {
            return PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                $"Runtime PLC channel '{result.PlcId}' was not found.");
        }

        if (channel is not IWritableRuntimePlcChannel writable)
        {
            return PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                $"Runtime PLC channel '{result.PlcId}' is not writable.");
        }

        RuntimePlcChannelState previous = writable.GetRuntimeState();
        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);
        var update = new PollingChannelUpdate(result.PlcId, next);
        PollingPublishResult publishResult = await _publishCoordinator.PublishAsync(
            [update],
            cancellationToken).ConfigureAwait(false);

        return publishResult.PublishSucceeded
            ? PollingResultStateOrchestrationResult.Success(result.PlcId, publishResult)
            : PollingResultStateOrchestrationResult.Failure(
                result.PlcId,
                publishResult.PublishException?.Message ?? "Polling publish did not succeed.",
                publishResult);
    }
}

public sealed class PollingResultStateOrchestrationResult
{
    private PollingResultStateOrchestrationResult(
        string plcId,
        bool succeeded,
        string? errorMessage,
        PollingPublishResult? publishResult)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        PlcId = plcId;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
        PublishResult = publishResult;
    }

    public string PlcId { get; }

    public bool Succeeded { get; }

    public string? ErrorMessage { get; }

    public PollingPublishResult? PublishResult { get; }

    internal static PollingResultStateOrchestrationResult Success(
        string plcId,
        PollingPublishResult publishResult)
        => new(
            plcId,
            succeeded: true,
            errorMessage: null,
            publishResult: publishResult ?? throw new ArgumentNullException(nameof(publishResult)));

    internal static PollingResultStateOrchestrationResult Failure(
        string plcId,
        string errorMessage,
        PollingPublishResult? publishResult = null)
        => new(
            plcId,
            succeeded: false,
            errorMessage: errorMessage ?? throw new ArgumentNullException(nameof(errorMessage)),
            publishResult: publishResult);
}

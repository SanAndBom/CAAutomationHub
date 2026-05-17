namespace CAAutomationHub.Runtime.Polling;

internal interface IPollingResultBatchPublisher
{
    Task<PollingResultStateOrchestrationBatchResult> PublishBatchAsync(
        IReadOnlyCollection<ChannelPollingResult> results,
        CancellationToken cancellationToken = default);
}

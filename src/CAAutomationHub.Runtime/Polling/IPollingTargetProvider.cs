namespace CAAutomationHub.Runtime.Polling;

public interface IPollingTargetProvider
{
    ValueTask<IReadOnlyCollection<ChannelPollingTarget>> GetTargetsAsync(
        CancellationToken cancellationToken = default);
}

using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Polling;

public sealed class RuntimeChannelPollingTargetProvider : IPollingTargetProvider
{
    private readonly RuntimeChannelRegistry _registry;

    public RuntimeChannelPollingTargetProvider(RuntimeChannelRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _registry = registry;
    }

    public ValueTask<IReadOnlyCollection<ChannelPollingTarget>> GetTargetsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<ChannelPollingTarget> targets = _registry
            .GetChannels()
            .Select(channel => new ChannelPollingTarget(channel.PlcId))
            .ToArray();

        return ValueTask.FromResult(targets);
    }
}

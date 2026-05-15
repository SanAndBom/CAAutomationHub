using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Channels;

public sealed class RuntimeChannelRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, IRuntimePlcChannel> _channelsByPlcId = new(StringComparer.Ordinal);

    public void Add(IRuntimePlcChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (string.IsNullOrWhiteSpace(channel.PlcId))
        {
            throw new ArgumentException("Channel PLC id must not be empty.", nameof(channel));
        }

        lock (_gate)
        {
            if (_channelsByPlcId.ContainsKey(channel.PlcId))
            {
                throw new InvalidOperationException($"A runtime PLC channel with PLC id '{channel.PlcId}' is already registered.");
            }

            _channelsByPlcId.Add(channel.PlcId, channel);
        }
    }

    public IReadOnlyList<IRuntimePlcChannel> GetChannels()
    {
        lock (_gate)
        {
            return _channelsByPlcId.Values.ToArray();
        }
    }

    public IReadOnlyList<ChannelRuntimeState> GetStates(DateTimeOffset capturedAt)
    {
        IRuntimePlcChannel[] channels;
        lock (_gate)
        {
            channels = _channelsByPlcId.Values.ToArray();
        }

        return channels
            .Select(channel => channel.GetState(capturedAt))
            .ToArray();
    }
}

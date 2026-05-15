using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Polling;

public sealed class PollingChannelUpdate
{
    public PollingChannelUpdate(string plcId, RuntimePlcChannelState state)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        PlcId = plcId;
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public string PlcId { get; }

    public RuntimePlcChannelState State { get; }
}

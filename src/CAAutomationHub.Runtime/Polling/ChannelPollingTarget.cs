namespace CAAutomationHub.Runtime.Polling;

/// <summary>
/// Vendor-neutral Runtime target for one PLC channel polling boundary.
/// </summary>
public sealed record ChannelPollingTarget
{
    public ChannelPollingTarget(string plcId)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id is required.", nameof(plcId));
        }

        PlcId = plcId;
    }

    public string PlcId { get; }
}

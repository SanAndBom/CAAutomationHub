using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Polling;

public static class RuntimePlcChannelStateMapper
{
    public static RuntimePlcChannelState Map(
        RuntimePlcChannelState previous,
        ChannelPollingResult result)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(result);

        if (!string.Equals(previous.PlcId, result.PlcId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Polling result PLC id '{result.PlcId}' does not match previous state PLC id '{previous.PlcId}'.",
                nameof(result));
        }

        return result.IsSuccess
            ? MapSuccess(previous, result)
            : MapFailure(previous, result);
    }

    private static RuntimePlcChannelState MapSuccess(
        RuntimePlcChannelState previous,
        ChannelPollingResult result)
        => previous with
        {
            LinkState = PlcLinkState.Online,
            HealthSeverity = PlcHealthSeverity.Healthy,
            PollingState = PlcPollingState.Polling,
            LastResponseMs = result.ResponseTimeMs ?? previous.LastResponseMs,
            ConsecutiveFailures = 0,
            LastSuccessAt = result.OccurredAt,
            LastError = null
        };

    private static RuntimePlcChannelState MapFailure(
        RuntimePlcChannelState previous,
        ChannelPollingResult result)
        => previous with
        {
            HealthSeverity = PlcHealthSeverity.Warning,
            PollingState = PlcPollingState.Delayed,
            LastResponseMs = result.ResponseTimeMs ?? previous.LastResponseMs,
            ConsecutiveFailures = previous.ConsecutiveFailures + 1,
            LastFailureAt = result.OccurredAt,
            LastError = result.ErrorMessage
        };
}

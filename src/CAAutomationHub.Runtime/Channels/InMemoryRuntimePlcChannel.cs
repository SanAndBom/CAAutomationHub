using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Channels;

public sealed class InMemoryRuntimePlcChannel : IWritableRuntimePlcChannel
{
    private readonly object _gate = new();
    private RuntimePlcChannelState _state;

    public InMemoryRuntimePlcChannel(
        string plcId,
        string plcName,
        string lineName = "",
        bool isEnabled = true,
        string ipAddress = "",
        int port = 0,
        PlcLinkState linkState = PlcLinkState.Online,
        PlcHealthSeverity healthSeverity = PlcHealthSeverity.Healthy,
        PlcPollingState pollingState = PlcPollingState.Polling,
        RuntimeSequenceState sequenceState = RuntimeSequenceState.Idle,
        int configuredPollingIntervalMs = 0,
        int effectivePollingIntervalMs = 0,
        int lastResponseMs = 0,
        int consecutiveFailures = 0,
        int reconnectCount = 0,
        double successRate = 1.0,
        string? lastError = null,
        DateTimeOffset? lastSuccessAt = null,
        DateTimeOffset? lastFailureAt = null)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id must not be empty.", nameof(plcId));
        }

        ArgumentNullException.ThrowIfNull(plcName);
        ArgumentNullException.ThrowIfNull(lineName);
        ArgumentNullException.ThrowIfNull(ipAddress);

        PlcId = plcId;
        _state = new RuntimePlcChannelState(
            PlcId: plcId,
            PlcName: plcName,
            LineName: lineName,
            IsEnabled: isEnabled,
            IpAddress: ipAddress,
            Port: port,
            LinkState: linkState,
            HealthSeverity: healthSeverity,
            PollingState: pollingState,
            SequenceState: sequenceState,
            ConfiguredPollingIntervalMs: configuredPollingIntervalMs,
            EffectivePollingIntervalMs: effectivePollingIntervalMs,
            LastResponseMs: lastResponseMs,
            ConsecutiveFailures: consecutiveFailures,
            ReconnectCount: reconnectCount,
            SuccessRate: successRate,
            LastSuccessAt: lastSuccessAt,
            LastFailureAt: lastFailureAt,
            LastError: lastError);
    }

    public string PlcId { get; }

    public void ReplaceState(RuntimePlcChannelState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(state.PlcId, PlcId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Replacement state PLC id '{state.PlcId}' does not match channel PLC id '{PlcId}'.",
                nameof(state));
        }

        lock (_gate)
        {
            _state = state;
        }
    }

    public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
    {
        _ = capturedAt;

        lock (_gate)
        {
            return _state.ToChannelRuntimeState();
        }
    }
}

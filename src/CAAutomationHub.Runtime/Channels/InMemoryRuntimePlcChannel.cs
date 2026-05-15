using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Channels;

public sealed class InMemoryRuntimePlcChannel : IRuntimePlcChannel
{
    private readonly string _plcName;
    private readonly string _lineName;
    private readonly bool _isEnabled;
    private readonly string _ipAddress;
    private readonly int _port;
    private readonly PlcLinkState _linkState;
    private readonly PlcHealthSeverity _healthSeverity;
    private readonly PlcPollingState _pollingState;
    private readonly RuntimeSequenceState _sequenceState;
    private readonly int _configuredPollingIntervalMs;
    private readonly int _effectivePollingIntervalMs;
    private readonly int _lastResponseMs;
    private readonly int _consecutiveFailures;
    private readonly int _reconnectCount;
    private readonly double _successRate;
    private readonly string? _lastError;

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
        string? lastError = null)
    {
        if (string.IsNullOrWhiteSpace(plcId))
        {
            throw new ArgumentException("PLC id must not be empty.", nameof(plcId));
        }

        ArgumentNullException.ThrowIfNull(plcName);
        ArgumentNullException.ThrowIfNull(lineName);
        ArgumentNullException.ThrowIfNull(ipAddress);

        PlcId = plcId;
        _plcName = plcName;
        _lineName = lineName;
        _isEnabled = isEnabled;
        _ipAddress = ipAddress;
        _port = port;
        _linkState = linkState;
        _healthSeverity = healthSeverity;
        _pollingState = pollingState;
        _sequenceState = sequenceState;
        _configuredPollingIntervalMs = configuredPollingIntervalMs;
        _effectivePollingIntervalMs = effectivePollingIntervalMs;
        _lastResponseMs = lastResponseMs;
        _consecutiveFailures = consecutiveFailures;
        _reconnectCount = reconnectCount;
        _successRate = successRate;
        _lastError = lastError;
    }

    public string PlcId { get; }

    public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
    {
        return new ChannelRuntimeState(
            PlcId: PlcId,
            PlcName: _plcName,
            LineName: _lineName,
            IsEnabled: _isEnabled,
            IpAddress: _ipAddress,
            Port: _port,
            LinkState: _linkState,
            HealthSeverity: _healthSeverity,
            PollingState: _pollingState,
            SequenceState: _sequenceState,
            ConfiguredPollingIntervalMs: _configuredPollingIntervalMs,
            EffectivePollingIntervalMs: _effectivePollingIntervalMs,
            LastResponseMs: _lastResponseMs,
            ConsecutiveFailures: _consecutiveFailures,
            ReconnectCount: _reconnectCount,
            SuccessRate: _successRate,
            LastSuccessAt: capturedAt,
            LastFailureAt: _lastError is null ? null : capturedAt,
            LastError: _lastError);
    }
}

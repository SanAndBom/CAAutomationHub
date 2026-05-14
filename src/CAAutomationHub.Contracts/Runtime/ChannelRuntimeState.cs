namespace CAAutomationHub.Contracts.Runtime;

public sealed record ChannelRuntimeState(
    string PlcId,
    string PlcName,
    string LineName,
    bool IsEnabled,
    string IpAddress,
    int Port,
    PlcLinkState LinkState,
    PlcHealthSeverity HealthSeverity,
    PlcPollingState PollingState,
    RuntimeSequenceState SequenceState,
    int ConfiguredPollingIntervalMs,
    int EffectivePollingIntervalMs,
    int LastResponseMs,
    int ConsecutiveFailures,
    int ReconnectCount,
    double SuccessRate,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastFailureAt,
    string? LastError);

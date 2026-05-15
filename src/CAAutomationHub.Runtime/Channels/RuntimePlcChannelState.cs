using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Channels;

/// <summary>
/// Runtime-local state model used as the source for a PLC channel snapshot before
/// it is published as a Contracts DTO.
/// </summary>
public sealed record RuntimePlcChannelState
{
    public RuntimePlcChannelState(
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
        string? LastError)
    {
        if (string.IsNullOrWhiteSpace(PlcId))
        {
            throw new ArgumentException("PLC id must not be empty.", nameof(PlcId));
        }

        ArgumentNullException.ThrowIfNull(PlcName);
        ArgumentNullException.ThrowIfNull(LineName);
        ArgumentNullException.ThrowIfNull(IpAddress);

        this.PlcId = PlcId;
        this.PlcName = PlcName;
        this.LineName = LineName;
        this.IsEnabled = IsEnabled;
        this.IpAddress = IpAddress;
        this.Port = Port;
        this.LinkState = LinkState;
        this.HealthSeverity = HealthSeverity;
        this.PollingState = PollingState;
        this.SequenceState = SequenceState;
        this.ConfiguredPollingIntervalMs = ConfiguredPollingIntervalMs;
        this.EffectivePollingIntervalMs = EffectivePollingIntervalMs;
        this.LastResponseMs = LastResponseMs;
        this.ConsecutiveFailures = ConsecutiveFailures;
        this.ReconnectCount = ReconnectCount;
        this.SuccessRate = SuccessRate;
        this.LastSuccessAt = LastSuccessAt;
        this.LastFailureAt = LastFailureAt;
        this.LastError = LastError;
    }

    public string PlcId { get; init; }
    public string PlcName { get; init; }
    public string LineName { get; init; }
    public bool IsEnabled { get; init; }
    public string IpAddress { get; init; }
    public int Port { get; init; }
    public PlcLinkState LinkState { get; init; }
    public PlcHealthSeverity HealthSeverity { get; init; }
    public PlcPollingState PollingState { get; init; }
    public RuntimeSequenceState SequenceState { get; init; }
    public int ConfiguredPollingIntervalMs { get; init; }
    public int EffectivePollingIntervalMs { get; init; }
    public int LastResponseMs { get; init; }
    public int ConsecutiveFailures { get; init; }
    public int ReconnectCount { get; init; }
    public double SuccessRate { get; init; }
    public DateTimeOffset? LastSuccessAt { get; init; }
    public DateTimeOffset? LastFailureAt { get; init; }
    public string? LastError { get; init; }

    internal ChannelRuntimeState ToChannelRuntimeState()
        => new(
            PlcId: PlcId,
            PlcName: PlcName,
            LineName: LineName,
            IsEnabled: IsEnabled,
            IpAddress: IpAddress,
            Port: Port,
            LinkState: LinkState,
            HealthSeverity: HealthSeverity,
            PollingState: PollingState,
            SequenceState: SequenceState,
            ConfiguredPollingIntervalMs: ConfiguredPollingIntervalMs,
            EffectivePollingIntervalMs: EffectivePollingIntervalMs,
            LastResponseMs: LastResponseMs,
            ConsecutiveFailures: ConsecutiveFailures,
            ReconnectCount: ReconnectCount,
            SuccessRate: SuccessRate,
            LastSuccessAt: LastSuccessAt,
            LastFailureAt: LastFailureAt,
            LastError: LastError);
}

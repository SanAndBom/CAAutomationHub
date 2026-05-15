using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Tests.Channels;

public sealed class InMemoryRuntimePlcChannelTests
{
    [Fact]
    public void ReplaceState_ThrowsWhenStateIsNull()
    {
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC");

        var exception = Assert.Throws<ArgumentNullException>(
            () => channel.ReplaceState(null!));

        Assert.Equal("state", exception.ParamName);
    }

    [Fact]
    public void ReplaceState_ThrowsWhenStatePlcIdDoesNotMatchChannelPlcId()
    {
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC");
        var replacement = CreateReplacementState(plcId: "PLC-02");

        var exception = Assert.Throws<ArgumentException>(
            () => channel.ReplaceState(replacement));

        Assert.Equal("state", exception.ParamName);
    }

    [Fact]
    public void ReplaceState_ReplacesInternalStateReturnedByGetState()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var lastSuccessAt = new DateTimeOffset(2026, 5, 15, 9, 30, 0, TimeSpan.Zero);
        var lastFailureAt = new DateTimeOffset(2026, 5, 15, 9, 45, 0, TimeSpan.Zero);
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            linkState: PlcLinkState.Online,
            healthSeverity: PlcHealthSeverity.Healthy,
            pollingState: PlcPollingState.Polling,
            sequenceState: RuntimeSequenceState.Idle);
        var replacement = CreateReplacementState(
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning,
            pollingState: PlcPollingState.Delayed,
            sequenceState: RuntimeSequenceState.Waiting,
            lastSuccessAt: lastSuccessAt,
            lastFailureAt: lastFailureAt);

        channel.ReplaceState(replacement);
        ChannelRuntimeState state = channel.GetState(capturedAt);

        Assert.Equal("PLC-01", state.PlcId);
        Assert.Equal("Packaging PLC", state.PlcName);
        Assert.Equal("Line-B", state.LineName);
        Assert.False(state.IsEnabled);
        Assert.Equal("10.0.0.42", state.IpAddress);
        Assert.Equal(2100, state.Port);
        Assert.Equal(PlcLinkState.Reconnecting, state.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, state.HealthSeverity);
        Assert.Equal(PlcPollingState.Delayed, state.PollingState);
        Assert.Equal(RuntimeSequenceState.Waiting, state.SequenceState);
        Assert.Equal(500, state.ConfiguredPollingIntervalMs);
        Assert.Equal(750, state.EffectivePollingIntervalMs);
        Assert.Equal(37, state.LastResponseMs);
        Assert.Equal(4, state.ConsecutiveFailures);
        Assert.Equal(2, state.ReconnectCount);
        Assert.Equal(0.95, state.SuccessRate);
        Assert.Equal(lastSuccessAt, state.LastSuccessAt);
        Assert.Equal(lastFailureAt, state.LastFailureAt);
        Assert.NotEqual(capturedAt, state.LastSuccessAt);
        Assert.NotEqual(capturedAt, state.LastFailureAt);
        Assert.Equal("Timeout", state.LastError);
    }

    [Fact]
    public void IRuntimePlcChannel_DoesNotExposeReplaceState()
    {
        Assert.Null(typeof(IRuntimePlcChannel).GetMethod("ReplaceState"));
    }

    [Fact]
    public void GetState_ReturnsChannelRuntimeStateWithIdentity()
    {
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            lineName: "Line-A",
            ipAddress: "192.168.0.10",
            port: 2004);

        ChannelRuntimeState state = channel.GetState(new DateTimeOffset(2026, 5, 15, 9, 0, 0, TimeSpan.Zero));

        Assert.Equal("PLC-01", channel.PlcId);
        Assert.Equal("PLC-01", state.PlcId);
        Assert.Equal("Cutting PLC", state.PlcName);
        Assert.Equal("Line-A", state.LineName);
        Assert.Equal("192.168.0.10", state.IpAddress);
        Assert.Equal(2004, state.Port);
    }

    [Fact]
    public void GetState_DoesNotOverwriteLastSuccessAtWithCapturedAt()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 9, 1, 0, TimeSpan.Zero);
        var lastSuccessAt = new DateTimeOffset(2026, 5, 15, 8, 45, 0, TimeSpan.Zero);
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            lastSuccessAt: lastSuccessAt);

        ChannelRuntimeState state = channel.GetState(capturedAt);

        Assert.Equal(lastSuccessAt, state.LastSuccessAt);
        Assert.NotEqual(capturedAt, state.LastSuccessAt);
    }

    [Fact]
    public void GetState_DoesNotOverwriteLastFailureAtWithCapturedAt()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 9, 1, 0, TimeSpan.Zero);
        var lastFailureAt = new DateTimeOffset(2026, 5, 15, 8, 50, 0, TimeSpan.Zero);
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            lastFailureAt: lastFailureAt,
            lastError: "Timeout");

        ChannelRuntimeState state = channel.GetState(capturedAt);

        Assert.Equal(lastFailureAt, state.LastFailureAt);
        Assert.NotEqual(capturedAt, state.LastFailureAt);
    }

    [Fact]
    public void GetState_KeepsDefaultEventTimestampsNullWhenUnset()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 9, 1, 0, TimeSpan.Zero);
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            lastError: "Timeout");

        ChannelRuntimeState state = channel.GetState(capturedAt);

        Assert.Null(state.LastSuccessAt);
        Assert.Null(state.LastFailureAt);
    }

    [Fact]
    public void GetState_ReturnsConfiguredRuntimeStateValues()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 9, 2, 0, TimeSpan.Zero);
        var lastSuccessAt = new DateTimeOffset(2026, 5, 15, 8, 30, 0, TimeSpan.Zero);
        var lastFailureAt = new DateTimeOffset(2026, 5, 15, 8, 40, 0, TimeSpan.Zero);
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-02",
            plcName: "Press PLC",
            lineName: "Line-B",
            isEnabled: false,
            ipAddress: "10.0.0.42",
            port: 2100,
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning,
            pollingState: PlcPollingState.Delayed,
            sequenceState: RuntimeSequenceState.Waiting,
            configuredPollingIntervalMs: 500,
            effectivePollingIntervalMs: 750,
            lastResponseMs: 37,
            consecutiveFailures: 4,
            reconnectCount: 2,
            successRate: 0.95,
            lastSuccessAt: lastSuccessAt,
            lastFailureAt: lastFailureAt,
            lastError: "Timeout");

        ChannelRuntimeState state = channel.GetState(capturedAt);

        Assert.False(state.IsEnabled);
        Assert.Equal(PlcLinkState.Reconnecting, state.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, state.HealthSeverity);
        Assert.Equal(PlcPollingState.Delayed, state.PollingState);
        Assert.Equal(RuntimeSequenceState.Waiting, state.SequenceState);
        Assert.Equal(500, state.ConfiguredPollingIntervalMs);
        Assert.Equal(750, state.EffectivePollingIntervalMs);
        Assert.Equal(37, state.LastResponseMs);
        Assert.Equal(4, state.ConsecutiveFailures);
        Assert.Equal(2, state.ReconnectCount);
        Assert.Equal(0.95, state.SuccessRate);
        Assert.Equal(lastSuccessAt, state.LastSuccessAt);
        Assert.Equal(lastFailureAt, state.LastFailureAt);
        Assert.Equal("Timeout", state.LastError);
    }

    private static InMemoryRuntimePlcChannelState CreateReplacementState(
        string plcId = "PLC-01",
        PlcLinkState linkState = PlcLinkState.Reconnecting,
        PlcHealthSeverity healthSeverity = PlcHealthSeverity.Warning,
        PlcPollingState pollingState = PlcPollingState.Delayed,
        RuntimeSequenceState sequenceState = RuntimeSequenceState.Waiting,
        DateTimeOffset? lastSuccessAt = null,
        DateTimeOffset? lastFailureAt = null)
        => new(
            PlcId: plcId,
            PlcName: "Packaging PLC",
            LineName: "Line-B",
            IsEnabled: false,
            IpAddress: "10.0.0.42",
            Port: 2100,
            LinkState: linkState,
            HealthSeverity: healthSeverity,
            PollingState: pollingState,
            SequenceState: sequenceState,
            ConfiguredPollingIntervalMs: 500,
            EffectivePollingIntervalMs: 750,
            LastResponseMs: 37,
            ConsecutiveFailures: 4,
            ReconnectCount: 2,
            SuccessRate: 0.95,
            LastSuccessAt: lastSuccessAt,
            LastFailureAt: lastFailureAt,
            LastError: "Timeout");
}

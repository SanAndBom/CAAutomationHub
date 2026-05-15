using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class RuntimePlcChannelStateMapperTests
{
    [Fact]
    public void MapSuccess_UsesOccurredAtAsLastSuccessAt()
    {
        RuntimePlcChannelState previous = CreatePreviousState(consecutiveFailures: 3, lastError: "Previous failure");
        var occurredAt = new DateTimeOffset(2026, 5, 16, 9, 0, 0, TimeSpan.Zero);
        var result = ChannelPollingResult.Success("PLC-01", occurredAt, responseTimeMs: 18);

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(occurredAt, next.LastSuccessAt);
    }

    [Fact]
    public void MapSuccess_ResetsConsecutiveFailures()
    {
        RuntimePlcChannelState previous = CreatePreviousState(consecutiveFailures: 3, lastError: "Previous failure");
        var result = ChannelPollingResult.Success(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 1, 0, TimeSpan.Zero),
            responseTimeMs: 18);

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(0, next.ConsecutiveFailures);
    }

    [Fact]
    public void MapSuccess_UpdatesLastResponseMsAndClearsLastError()
    {
        RuntimePlcChannelState previous = CreatePreviousState(lastResponseMs: 37, lastError: "Previous failure");
        var result = ChannelPollingResult.Success(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 2, 0, TimeSpan.Zero),
            responseTimeMs: 18);

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(18, next.LastResponseMs);
        Assert.Null(next.LastError);
    }

    [Fact]
    public void MapSuccess_MapsToOnlineHealthyPollingState()
    {
        RuntimePlcChannelState previous = CreatePreviousState(
            linkState: PlcLinkState.Reconnecting,
            healthSeverity: PlcHealthSeverity.Warning,
            pollingState: PlcPollingState.Delayed);
        var result = ChannelPollingResult.Success(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 3, 0, TimeSpan.Zero),
            responseTimeMs: 18);

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(PlcLinkState.Online, next.LinkState);
        Assert.Equal(PlcHealthSeverity.Healthy, next.HealthSeverity);
        Assert.Equal(PlcPollingState.Polling, next.PollingState);
    }

    [Fact]
    public void MapFailure_UsesOccurredAtAsLastFailureAt()
    {
        RuntimePlcChannelState previous = CreatePreviousState();
        var occurredAt = new DateTimeOffset(2026, 5, 16, 9, 4, 0, TimeSpan.Zero);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            occurredAt,
            ChannelPollingFailureKind.Timeout,
            "Polling timed out.");

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(occurredAt, next.LastFailureAt);
    }

    [Fact]
    public void MapFailure_IncrementsConsecutiveFailures()
    {
        RuntimePlcChannelState previous = CreatePreviousState(consecutiveFailures: 3);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 5, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.Connection,
            "Connection unavailable.");

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(4, next.ConsecutiveFailures);
    }

    [Fact]
    public void MapFailure_PreservesPreviousLastSuccessAt()
    {
        var lastSuccessAt = new DateTimeOffset(2026, 5, 16, 8, 30, 0, TimeSpan.Zero);
        RuntimePlcChannelState previous = CreatePreviousState(lastSuccessAt: lastSuccessAt);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 6, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.Protocol,
            "Unexpected protocol response.");

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(lastSuccessAt, next.LastSuccessAt);
    }

    [Fact]
    public void MapFailure_PreservesLastResponseMsWhenResponseTimeIsNull()
    {
        RuntimePlcChannelState previous = CreatePreviousState(lastResponseMs: 37);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 7, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.UnexpectedResponse,
            "Unexpected response.",
            responseTimeMs: null);

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(37, next.LastResponseMs);
    }

    [Fact]
    public void MapFailure_MapsToWarningDelayedAndPreservesLinkState()
    {
        RuntimePlcChannelState previous = CreatePreviousState(linkState: PlcLinkState.Online);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 8, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.Unknown,
            "Unknown polling failure.");

        RuntimePlcChannelState next = RuntimePlcChannelStateMapper.Map(previous, result);

        Assert.Equal(PlcLinkState.Online, next.LinkState);
        Assert.Equal(PlcHealthSeverity.Warning, next.HealthSeverity);
        Assert.Equal(PlcPollingState.Delayed, next.PollingState);
    }

    [Fact]
    public void Map_ThrowsWhenResultPlcIdDoesNotMatchPreviousState()
    {
        RuntimePlcChannelState previous = CreatePreviousState(plcId: "PLC-01");
        var result = ChannelPollingResult.Success(
            "PLC-02",
            new DateTimeOffset(2026, 5, 16, 9, 9, 0, TimeSpan.Zero),
            responseTimeMs: 18);

        var exception = Assert.Throws<ArgumentException>(
            () => RuntimePlcChannelStateMapper.Map(previous, result));

        Assert.Equal("result", exception.ParamName);
    }

    [Fact]
    public void Mapper_DoesNotRequireRegistrySupervisorOrPublishDependencies()
    {
        Type mapperType = typeof(RuntimePlcChannelStateMapper);
        Type[] parameterTypes = mapperType
            .GetMethod(nameof(RuntimePlcChannelStateMapper.Map))!
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Equal([typeof(RuntimePlcChannelState), typeof(ChannelPollingResult)], parameterTypes);
    }

    private static RuntimePlcChannelState CreatePreviousState(
        string plcId = "PLC-01",
        PlcLinkState linkState = PlcLinkState.Online,
        PlcHealthSeverity healthSeverity = PlcHealthSeverity.Healthy,
        PlcPollingState pollingState = PlcPollingState.Polling,
        int lastResponseMs = 12,
        int consecutiveFailures = 0,
        DateTimeOffset? lastSuccessAt = null,
        DateTimeOffset? lastFailureAt = null,
        string? lastError = null)
        => new(
            PlcId: plcId,
            PlcName: "Cutting PLC",
            LineName: "Line-A",
            IsEnabled: true,
            IpAddress: "192.168.0.10",
            Port: 2004,
            LinkState: linkState,
            HealthSeverity: healthSeverity,
            PollingState: pollingState,
            SequenceState: RuntimeSequenceState.Idle,
            ConfiguredPollingIntervalMs: 500,
            EffectivePollingIntervalMs: 500,
            LastResponseMs: lastResponseMs,
            ConsecutiveFailures: consecutiveFailures,
            ReconnectCount: 2,
            SuccessRate: 0.95,
            LastSuccessAt: lastSuccessAt,
            LastFailureAt: lastFailureAt,
            LastError: lastError);
}

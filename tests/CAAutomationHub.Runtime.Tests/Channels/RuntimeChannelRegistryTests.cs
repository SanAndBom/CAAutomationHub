using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

namespace CAAutomationHub.Runtime.Tests.Channels;

public sealed class RuntimeChannelRegistryTests
{
    [Fact]
    public void Add_ThrowsArgumentNullExceptionWhenChannelIsNull()
    {
        var registry = new RuntimeChannelRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Add(null!));
    }

    [Fact]
    public void Add_IncludesChannelInSnapshotCopy()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new StubRuntimePlcChannel("PLC-01");

        registry.Add(channel);

        IReadOnlyList<IRuntimePlcChannel> channels = registry.GetChannels();
        Assert.Single(channels);
        Assert.Same(channel, channels[0]);
    }

    [Fact]
    public void Add_ThrowsInvalidOperationExceptionWhenPlcIdAlreadyExists()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new StubRuntimePlcChannel("PLC-01"));

        Assert.Throws<InvalidOperationException>(() => registry.Add(new StubRuntimePlcChannel("PLC-01")));
    }

    [Fact]
    public void GetChannels_ReturnsSnapshotCopy()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new StubRuntimePlcChannel("PLC-01"));

        IReadOnlyList<IRuntimePlcChannel> firstSnapshot = registry.GetChannels();
        registry.Add(new StubRuntimePlcChannel("PLC-02"));

        Assert.Single(firstSnapshot);
        Assert.Equal(2, registry.GetChannels().Count);
    }

    [Fact]
    public void GetStates_ReturnsStatesForAllChannels()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
        var registry = new RuntimeChannelRegistry();
        registry.Add(new StubRuntimePlcChannel("PLC-01"));
        registry.Add(new StubRuntimePlcChannel("PLC-02"));

        IReadOnlyList<ChannelRuntimeState> states = registry.GetStates(capturedAt);

        Assert.Equal(["PLC-01", "PLC-02"], states.Select(state => state.PlcId));
    }

    [Fact]
    public void GetStates_PassesCapturedAtWithoutChangingEventTimestamps()
    {
        var capturedAt = new DateTimeOffset(2026, 5, 15, 10, 1, 0, TimeSpan.Zero);
        var firstLastSuccessAt = new DateTimeOffset(2026, 5, 15, 9, 40, 0, TimeSpan.Zero);
        var firstLastFailureAt = new DateTimeOffset(2026, 5, 15, 9, 45, 0, TimeSpan.Zero);
        var secondLastSuccessAt = new DateTimeOffset(2026, 5, 15, 9, 50, 0, TimeSpan.Zero);
        var registry = new RuntimeChannelRegistry();
        var first = new StubRuntimePlcChannel(
            "PLC-01",
            firstLastSuccessAt,
            firstLastFailureAt);
        var second = new StubRuntimePlcChannel(
            "PLC-02",
            secondLastSuccessAt,
            lastFailureAt: null);
        registry.Add(first);
        registry.Add(second);

        IReadOnlyList<ChannelRuntimeState> states = registry.GetStates(capturedAt);

        Assert.Equal(capturedAt, first.LastCapturedAt);
        Assert.Equal(capturedAt, second.LastCapturedAt);
        Assert.Collection(
            states,
            state =>
            {
                Assert.Equal(firstLastSuccessAt, state.LastSuccessAt);
                Assert.Equal(firstLastFailureAt, state.LastFailureAt);
            },
            state =>
            {
                Assert.Equal(secondLastSuccessAt, state.LastSuccessAt);
                Assert.Null(state.LastFailureAt);
            });
    }

    private sealed class StubRuntimePlcChannel : IRuntimePlcChannel
    {
        private readonly DateTimeOffset? _lastSuccessAt;
        private readonly DateTimeOffset? _lastFailureAt;

        public StubRuntimePlcChannel(
            string plcId,
            DateTimeOffset? lastSuccessAt = null,
            DateTimeOffset? lastFailureAt = null)
        {
            PlcId = plcId;
            _lastSuccessAt = lastSuccessAt;
            _lastFailureAt = lastFailureAt;
        }

        public string PlcId { get; }

        public DateTimeOffset? LastCapturedAt { get; private set; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
        {
            LastCapturedAt = capturedAt;

            return new ChannelRuntimeState(
                PlcId: PlcId,
                PlcName: $"{PlcId} name",
                LineName: "Line-A",
                IsEnabled: true,
                IpAddress: "127.0.0.1",
                Port: 2004,
                LinkState: PlcLinkState.Online,
                HealthSeverity: PlcHealthSeverity.Healthy,
                PollingState: PlcPollingState.Polling,
                SequenceState: RuntimeSequenceState.Idle,
                ConfiguredPollingIntervalMs: 500,
                EffectivePollingIntervalMs: 500,
                LastResponseMs: 0,
                ConsecutiveFailures: 0,
                ReconnectCount: 0,
                SuccessRate: 1.0,
                LastSuccessAt: _lastSuccessAt,
                LastFailureAt: _lastFailureAt,
                LastError: null);
        }
    }
}

using System.Reflection;
using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingTargetProviderTests
{
    [Fact]
    public async Task GetTargetsAsync_ReturnsPlcLevelTargetsFromRegisteredChannels()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-01", "Cutting PLC"));
        registry.Add(new InMemoryRuntimePlcChannel("PLC-02", "Packaging PLC"));
        var provider = new RuntimeChannelPollingTargetProvider(registry);

        IReadOnlyCollection<ChannelPollingTarget> targets = await provider.GetTargetsAsync(CancellationToken.None);

        Assert.Equal(2, targets.Count);
        Assert.Equal(["PLC-01", "PLC-02"], targets.Select(target => target.PlcId).OrderBy(plcId => plcId));
        Assert.Equal(
            ["PlcId"],
            typeof(ChannelPollingTarget)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name));
    }

    [Fact]
    public async Task GetTargetsAsync_WhenRegistryEmpty_ReturnsEmptyCollection()
    {
        var registry = new RuntimeChannelRegistry();
        var provider = new RuntimeChannelPollingTargetProvider(registry);

        IReadOnlyCollection<ChannelPollingTarget> targets = await provider.GetTargetsAsync(CancellationToken.None);

        Assert.NotNull(targets);
        Assert.Empty(targets);
    }

    [Fact]
    public void Constructor_WhenRegistryNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RuntimeChannelPollingTargetProvider(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChannelPollingTarget_RequiresPlcId(string? plcId)
    {
        Assert.Throws<ArgumentException>(() => new ChannelPollingTarget(plcId!));
    }

    [Fact]
    public async Task GetTargetsAsync_DoesNotMutateRegistryOrReadChannelState()
    {
        var registry = new RuntimeChannelRegistry();
        var first = new TrackingRuntimePlcChannel("PLC-01");
        var second = new TrackingRuntimePlcChannel("PLC-02");
        registry.Add(first);
        registry.Add(second);
        var provider = new RuntimeChannelPollingTargetProvider(registry);
        int beforeCount = registry.GetChannels().Count;

        IReadOnlyCollection<ChannelPollingTarget> targets = await provider.GetTargetsAsync(CancellationToken.None);

        Assert.Equal(beforeCount, registry.GetChannels().Count);
        Assert.Equal(2, targets.Count);
        Assert.Equal(0, first.GetStateCallCount);
        Assert.Equal(0, second.GetStateCallCount);
    }

    private sealed class TrackingRuntimePlcChannel : IRuntimePlcChannel
    {
        public TrackingRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public int GetStateCallCount { get; private set; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
        {
            _ = capturedAt;
            GetStateCallCount++;

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
                LastSuccessAt: null,
                LastFailureAt: null,
                LastError: null);
        }
    }
}

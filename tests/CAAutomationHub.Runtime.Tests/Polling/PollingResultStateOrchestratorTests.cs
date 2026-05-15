using System.Reflection;
using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;
using CAAutomationHub.Runtime.Polling;

namespace CAAutomationHub.Runtime.Tests.Polling;

public sealed class PollingResultStateOrchestratorTests
{
    [Fact]
    public void Constructor_ThrowsWhenChannelRegistryIsNull()
    {
        var coordinator = new PollingPublishCoordinator(
            new RuntimeChannelRegistry(),
            _ => Task.FromResult(RuntimeSnapshot.Empty));

        var exception = Assert.Throws<ArgumentNullException>(
            () => new PollingResultStateOrchestrator(null!, coordinator));

        Assert.Equal("channelRegistry", exception.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsWhenPublishCoordinatorIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new PollingResultStateOrchestrator(new RuntimeChannelRegistry(), null!));

        Assert.Equal("publishCoordinator", exception.ParamName);
    }

    [Fact]
    public async Task PublishAsync_WhenChannelMissing_ReturnsFailure()
    {
        var registry = new RuntimeChannelRegistry();
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => throw new InvalidOperationException("Refresh should not be called."));
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var result = ChannelPollingResult.Success(
            "PLC-MISSING",
            new DateTimeOffset(2026, 5, 16, 9, 0, 0, TimeSpan.Zero),
            responseTimeMs: 18);

        PollingResultStateOrchestrationResult orchestrationResult = await orchestrator.PublishAsync(
            result,
            CancellationToken.None);

        Assert.Equal("PLC-MISSING", orchestrationResult.PlcId);
        Assert.False(orchestrationResult.Succeeded);
        Assert.NotNull(orchestrationResult.ErrorMessage);
        Assert.Null(orchestrationResult.PublishResult);
    }

    [Fact]
    public async Task PublishAsync_WhenChannelIsNotWritable_ReturnsFailure()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new ReadOnlyRuntimePlcChannel("PLC-READONLY"));
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => throw new InvalidOperationException("Refresh should not be called."));
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var result = ChannelPollingResult.Failure(
            "PLC-READONLY",
            new DateTimeOffset(2026, 5, 16, 9, 1, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.Timeout,
            "Polling timed out.");

        PollingResultStateOrchestrationResult orchestrationResult = await orchestrator.PublishAsync(
            result,
            CancellationToken.None);

        Assert.Equal("PLC-READONLY", orchestrationResult.PlcId);
        Assert.False(orchestrationResult.Succeeded);
        Assert.NotNull(orchestrationResult.ErrorMessage);
        Assert.Null(orchestrationResult.PublishResult);
    }

    [Fact]
    public async Task PublishAsync_WhenWritableChannelExists_MapsPreviousStateAndDelegatesToCoordinator()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel(
            plcId: "PLC-01",
            plcName: "Cutting PLC",
            consecutiveFailures: 3,
            lastError: "Previous timeout");
        registry.Add(channel);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var occurredAt = new DateTimeOffset(2026, 5, 16, 9, 2, 0, TimeSpan.Zero);
        var result = ChannelPollingResult.Success("PLC-01", occurredAt, responseTimeMs: 18);

        PollingResultStateOrchestrationResult orchestrationResult = await orchestrator.PublishAsync(
            result,
            CancellationToken.None);

        RuntimePlcChannelState next = channel.GetRuntimeState();
        Assert.True(orchestrationResult.Succeeded);
        Assert.NotNull(orchestrationResult.PublishResult);
        Assert.True(orchestrationResult.PublishResult.PublishSucceeded);
        Assert.Equal(1, orchestrationResult.PublishResult.UpdatedCount);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal(occurredAt, next.LastSuccessAt);
        Assert.Equal(18, next.LastResponseMs);
        Assert.Equal(0, next.ConsecutiveFailures);
        Assert.Null(next.LastError);
    }

    [Fact]
    public async Task PublishAsync_DoesNotCallReplaceStateDirectly()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new CountingWritableRuntimePlcChannel(CreateState("PLC-01"));
        registry.Add(channel);
        var refreshCallCount = 0;
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ =>
            {
                refreshCallCount++;
                return Task.FromResult(RuntimeSnapshot.Empty);
            });
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 3, 0, TimeSpan.Zero),
            ChannelPollingFailureKind.Connection,
            "Connection unavailable.");

        PollingResultStateOrchestrationResult orchestrationResult = await orchestrator.PublishAsync(
            result,
            CancellationToken.None);

        Assert.True(orchestrationResult.Succeeded);
        Assert.Equal(1, channel.ReplaceStateCallCount);
        Assert.Equal(1, refreshCallCount);
    }

    [Fact]
    public async Task PublishAsync_DoesNotUseCapturedAt()
    {
        var registry = new RuntimeChannelRegistry();
        var channel = new InMemoryRuntimePlcChannel("PLC-01", "Cutting PLC");
        registry.Add(channel);
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => Task.FromResult(RuntimeSnapshot.Empty));
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var occurredAt = new DateTimeOffset(2026, 5, 16, 9, 4, 0, TimeSpan.Zero);
        var result = ChannelPollingResult.Failure(
            "PLC-01",
            occurredAt,
            ChannelPollingFailureKind.Protocol,
            "Unexpected protocol response.");

        await orchestrator.PublishAsync(result, CancellationToken.None);

        MethodInfo publishMethod = typeof(PollingResultStateOrchestrator)
            .GetMethod(nameof(PollingResultStateOrchestrator.PublishAsync))!;
        Type[] parameterTypes = publishMethod
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        Assert.Equal([typeof(ChannelPollingResult), typeof(CancellationToken)], parameterTypes);
        Assert.Equal(occurredAt, channel.GetRuntimeState().LastFailureAt);
    }

    [Fact]
    public async Task PublishAsync_PropagatesCoordinatorPublishResult()
    {
        var registry = new RuntimeChannelRegistry();
        registry.Add(new InMemoryRuntimePlcChannel("PLC-01", "Cutting PLC"));
        var publishException = new InvalidOperationException("Snapshot refresh failed.");
        var coordinator = new PollingPublishCoordinator(
            registry,
            _ => throw publishException);
        var orchestrator = new PollingResultStateOrchestrator(registry, coordinator);
        var result = ChannelPollingResult.Success(
            "PLC-01",
            new DateTimeOffset(2026, 5, 16, 9, 5, 0, TimeSpan.Zero));

        PollingResultStateOrchestrationResult orchestrationResult = await orchestrator.PublishAsync(
            result,
            CancellationToken.None);

        Assert.False(orchestrationResult.Succeeded);
        Assert.NotNull(orchestrationResult.PublishResult);
        Assert.False(orchestrationResult.PublishResult.PublishSucceeded);
        Assert.Same(publishException, orchestrationResult.PublishResult.PublishException);
    }

    private static RuntimePlcChannelState CreateState(string plcId)
        => new(
            PlcId: plcId,
            PlcName: "Cutting PLC",
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
            LastResponseMs: 12,
            ConsecutiveFailures: 0,
            ReconnectCount: 0,
            SuccessRate: 1.0,
            LastSuccessAt: null,
            LastFailureAt: null,
            LastError: null);

    private sealed class ReadOnlyRuntimePlcChannel : IRuntimePlcChannel
    {
        public ReadOnlyRuntimePlcChannel(string plcId)
        {
            PlcId = plcId;
        }

        public string PlcId { get; }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => CreateState(PlcId).ToChannelRuntimeStateForTest();
    }

    private sealed class CountingWritableRuntimePlcChannel : IWritableRuntimePlcChannel
    {
        private RuntimePlcChannelState _state;

        public CountingWritableRuntimePlcChannel(RuntimePlcChannelState state)
        {
            _state = state;
            PlcId = state.PlcId;
        }

        public string PlcId { get; }

        public int ReplaceStateCallCount { get; private set; }

        public RuntimePlcChannelState GetRuntimeState()
            => _state;

        public void ReplaceState(RuntimePlcChannelState state)
        {
            ReplaceStateCallCount++;
            _state = state;
        }

        public ChannelRuntimeState GetState(DateTimeOffset capturedAt)
            => _state.ToChannelRuntimeStateForTest();
    }
}

internal static class RuntimePlcChannelStateTestExtensions
{
    public static ChannelRuntimeState ToChannelRuntimeStateForTest(this RuntimePlcChannelState state)
        => new(
            PlcId: state.PlcId,
            PlcName: state.PlcName,
            LineName: state.LineName,
            IsEnabled: state.IsEnabled,
            IpAddress: state.IpAddress,
            Port: state.Port,
            LinkState: state.LinkState,
            HealthSeverity: state.HealthSeverity,
            PollingState: state.PollingState,
            SequenceState: state.SequenceState,
            ConfiguredPollingIntervalMs: state.ConfiguredPollingIntervalMs,
            EffectivePollingIntervalMs: state.EffectivePollingIntervalMs,
            LastResponseMs: state.LastResponseMs,
            ConsecutiveFailures: state.ConsecutiveFailures,
            ReconnectCount: state.ReconnectCount,
            SuccessRate: state.SuccessRate,
            LastSuccessAt: state.LastSuccessAt,
            LastFailureAt: state.LastFailureAt,
            LastError: state.LastError);
}

using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime;
using CAAutomationHub.Wpf.Adapters;

namespace CAAutomationHub.Wpf.Tests.Adapters;

public sealed class SupervisorRuntimeDashboardLifecycleTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ThrowsArgumentNullExceptionWhenSupervisorIsNull()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);

        Assert.Throws<ArgumentNullException>(
            () => new SupervisorRuntimeDashboardLifecycle(null!, provider));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullExceptionWhenSnapshotProviderIsNull()
    {
        var supervisor = new StubAutomationHubSupervisor();

        Assert.Throws<ArgumentNullException>(
            () => new SupervisorRuntimeDashboardLifecycle(supervisor, null!));
    }

    [Fact]
    public void SupervisorRuntimeDashboardLifecycle_ImplementsRuntimeDashboardLifecycle()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);

        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);

        Assert.IsAssignableFrom<IRuntimeDashboardLifecycle>(lifecycle);
    }

    [Fact]
    public async Task StartAsync_StartsSupervisorBeforeRefreshingSnapshotProvider()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);
        supervisor.SnapshotToReturn = CreateSnapshot(CapturedAt, totalPlcs: 3);

        await lifecycle.StartAsync(CancellationToken.None);

        Assert.Equal(
            [nameof(IAutomationHubSupervisor.StartAsync), nameof(IAutomationHubSupervisor.GetSnapshotAsync)],
            supervisor.Calls);
        Assert.Same(supervisor.SnapshotToReturn, provider.GetSnapshot());
    }

    [Fact]
    public async Task StartAsync_PropagatesSupervisorStartFailureAndDoesNotRefresh()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);
        var failure = new InvalidOperationException("Start failed.");
        supervisor.StartAsyncException = failure;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => lifecycle.StartAsync(CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Equal([nameof(IAutomationHubSupervisor.StartAsync)], supervisor.Calls);
        Assert.Equal(0, supervisor.GetSnapshotAsyncCallCount);
    }

    [Fact]
    public async Task StartAsync_ToleratesInitialRefreshFailureAndKeepsProviderCache()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);
        RuntimeSnapshot existing = CreateSnapshot(CapturedAt, totalPlcs: 2);
        supervisor.PublishSnapshot(existing);
        supervisor.GetSnapshotAsyncException = new InvalidOperationException("Refresh failed.");

        await lifecycle.StartAsync(CancellationToken.None);

        Assert.Equal(
            [nameof(IAutomationHubSupervisor.StartAsync), nameof(IAutomationHubSupervisor.GetSnapshotAsync)],
            supervisor.Calls);
        Assert.Same(existing, provider.GetSnapshot());
    }

    [Fact]
    public async Task StopAsync_StopsSupervisor()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);

        await lifecycle.StopAsync(CancellationToken.None);

        Assert.Equal([nameof(IAutomationHubSupervisor.StopAsync)], supervisor.Calls);
    }

    [Fact]
    public async Task StopAsync_DoesNotDisposeSnapshotProvider()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        var lifecycle = new SupervisorRuntimeDashboardLifecycle(supervisor, provider);
        RuntimeSnapshot snapshot = CreateSnapshot(CapturedAt, totalPlcs: 4);

        await lifecycle.StopAsync(CancellationToken.None);
        supervisor.PublishSnapshot(snapshot);

        Assert.Same(snapshot, provider.GetSnapshot());
    }

    [Fact]
    public void RuntimeDashboardAdapter_DoesNotImplementRuntimeDashboardLifecycle()
    {
        object adapter = new RuntimeDashboardAdapter();

        Assert.False(adapter is IRuntimeDashboardLifecycle);
    }

    private static RuntimeSnapshot CreateSnapshot(DateTimeOffset capturedAt, int totalPlcs)
        => new(
            capturedAt,
            new RuntimeHealthState(
                TotalPlcs: totalPlcs,
                OnlineCount: totalPlcs,
                ReconnectingCount: 0,
                HealthyCount: totalPlcs,
                WarningCount: 0,
                CongestedCount: 0,
                ErrorCount: 0,
                InactiveCount: 0,
                CapturedAt: capturedAt),
            channels: [],
            recentEvents: []);

    private sealed class StubAutomationHubSupervisor : IAutomationHubSupervisor
    {
        private event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChangedCore;

        public event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged
        {
            add => SnapshotChangedCore += value;
            remove => SnapshotChangedCore -= value;
        }

        public event EventHandler<RuntimeEvent>? RuntimeEventRaised
        {
            add { }
            remove { }
        }

        public List<string> Calls { get; } = [];
        public int GetSnapshotAsyncCallCount { get; private set; }
        public RuntimeSnapshot SnapshotToReturn { get; set; } = RuntimeSnapshot.Empty;
        public Exception? StartAsyncException { get; set; }
        public Exception? GetSnapshotAsyncException { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(nameof(IAutomationHubSupervisor.StartAsync));
            if (StartAsyncException is not null)
            {
                throw StartAsyncException;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(nameof(IAutomationHubSupervisor.StopAsync));
            return Task.CompletedTask;
        }

        public Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(nameof(IAutomationHubSupervisor.GetSnapshotAsync));
            GetSnapshotAsyncCallCount++;
            if (GetSnapshotAsyncException is not null)
            {
                throw GetSnapshotAsyncException;
            }

            return Task.FromResult(SnapshotToReturn);
        }

        public Task<RuntimeDashboardCommandResult> ExecuteAsync(
            RuntimeDashboardCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new RuntimeDashboardCommandResult(
                command.CommandId,
                Success: false,
                Status: "NotImplemented",
                Message: "Test stub does not execute commands.",
                PlcId: command.PlcId,
                ErrorCode: null,
                CompletedAt: DateTimeOffset.UnixEpoch));
        }

        public void PublishSnapshot(RuntimeSnapshot snapshot)
        {
            SnapshotChangedCore?.Invoke(
                this,
                new RuntimeSnapshotChangedEventArgs(snapshot, snapshot.CapturedAt));
        }
    }
}

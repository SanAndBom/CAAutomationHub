using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime;
using CAAutomationHub.Wpf.Adapters;

namespace CAAutomationHub.Wpf.Tests.Adapters;

public sealed class SupervisorRuntimeSnapshotProviderTests
{
    private static readonly DateTimeOffset CapturedAt = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 5, 14, 12, 1, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ThrowsArgumentNullExceptionWhenSupervisorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SupervisorRuntimeSnapshotProvider(null!));
    }

    [Fact]
    public void GetSnapshot_ReturnsEmptyRuntimeSnapshotBeforeSupervisorPublishesSnapshot()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);

        RuntimeSnapshot snapshot = provider.GetSnapshot();

        Assert.Same(RuntimeSnapshot.Empty, snapshot);
    }

    [Fact]
    public void GetSnapshot_DoesNotCallSupervisorGetSnapshotAsync()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);

        provider.GetSnapshot();

        Assert.Equal(0, supervisor.GetSnapshotAsyncCallCount);
    }

    [Fact]
    public void SnapshotChanged_UpdatesCachedRuntimeSnapshot()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot expected = CreateSnapshot(CapturedAt, totalPlcs: 3);

        supervisor.PublishSnapshot(expected);

        Assert.Same(expected, provider.GetSnapshot());
    }

    [Fact]
    public void Dispose_UnsubscribesFromSupervisorSnapshotChanged()
    {
        var supervisor = new StubAutomationHubSupervisor();
        var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot first = CreateSnapshot(CapturedAt, totalPlcs: 1);
        RuntimeSnapshot second = CreateSnapshot(UpdatedAt, totalPlcs: 2);

        supervisor.PublishSnapshot(first);
        provider.Dispose();
        supervisor.PublishSnapshot(second);

        Assert.Same(first, provider.GetSnapshot());
    }

    [Fact]
    public void Provider_ExposesRuntimeSnapshotOnlyThroughRuntimeSnapshotProviderContract()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);

        Assert.IsAssignableFrom<IRuntimeSnapshotProvider>(provider);
        Assert.IsAssignableFrom<IDisposable>(provider);
        Assert.Equal(typeof(RuntimeSnapshot), provider.GetSnapshot().GetType());
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
        public event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged;
        public event EventHandler<RuntimeEvent>? RuntimeEventRaised
        {
            add { }
            remove { }
        }

        public int GetSnapshotAsyncCallCount { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetSnapshotAsyncCallCount++;
            return Task.FromResult(RuntimeSnapshot.Empty);
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
            SnapshotChanged?.Invoke(
                this,
                new RuntimeSnapshotChangedEventArgs(snapshot, snapshot.CapturedAt));
        }

    }
}

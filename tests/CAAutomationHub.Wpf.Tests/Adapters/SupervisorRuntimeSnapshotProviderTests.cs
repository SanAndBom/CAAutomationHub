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
    public async Task RefreshAsync_UpdatesCacheWithSupervisorSnapshotAndReturnsCachedSnapshot()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot expected = CreateSnapshot(CapturedAt, totalPlcs: 3);
        supervisor.SnapshotToReturn = expected;

        RuntimeSnapshot refreshed = await provider.RefreshAsync(CancellationToken.None);

        Assert.Same(expected, refreshed);
        Assert.Same(expected, provider.GetSnapshot());
        Assert.Equal(1, supervisor.GetSnapshotAsyncCallCount);
    }

    [Fact]
    public async Task GetSnapshot_ReturnsCachedRuntimeSnapshotAfterRefreshAsync()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot expected = CreateSnapshot(CapturedAt, totalPlcs: 4);
        supervisor.SnapshotToReturn = expected;

        await provider.RefreshAsync(CancellationToken.None);

        Assert.Same(expected, provider.GetSnapshot());
        Assert.Equal(1, supervisor.GetSnapshotAsyncCallCount);
    }

    [Fact]
    public async Task RefreshAsync_KeepsExistingCacheAndRethrowsWhenSupervisorGetSnapshotAsyncFails()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot existing = CreateSnapshot(CapturedAt, totalPlcs: 2);
        var failure = new InvalidOperationException("Snapshot refresh failed.");
        supervisor.PublishSnapshot(existing);
        supervisor.GetSnapshotAsyncException = failure;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.RefreshAsync(CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Same(existing, provider.GetSnapshot());
        Assert.Equal(1, supervisor.GetSnapshotAsyncCallCount);
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
    public async Task RefreshAsync_DoesNotOverwriteNewerSnapshotWithOlderSupervisorSnapshot()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot newer = CreateSnapshot(UpdatedAt, totalPlcs: 5);
        RuntimeSnapshot older = CreateSnapshot(CapturedAt, totalPlcs: 1);
        supervisor.PublishSnapshot(newer);
        supervisor.SnapshotToReturn = older;

        RuntimeSnapshot refreshed = await provider.RefreshAsync(CancellationToken.None);

        Assert.Same(newer, refreshed);
        Assert.Same(newer, provider.GetSnapshot());
    }

    [Fact]
    public async Task SnapshotChanged_DoesNotOverwriteNewerSnapshotWithOlderEventSnapshot()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot newer = CreateSnapshot(UpdatedAt, totalPlcs: 5);
        RuntimeSnapshot older = CreateSnapshot(CapturedAt, totalPlcs: 1);
        supervisor.SnapshotToReturn = newer;
        await provider.RefreshAsync(CancellationToken.None);

        supervisor.PublishSnapshot(older);

        Assert.Same(newer, provider.GetSnapshot());
    }

    [Fact]
    public async Task SameCapturedAtSnapshotAllowsLastArrivalToReplaceCache()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        RuntimeSnapshot first = CreateSnapshot(CapturedAt, totalPlcs: 1);
        RuntimeSnapshot second = CreateSnapshot(CapturedAt, totalPlcs: 2);
        supervisor.PublishSnapshot(first);
        supervisor.SnapshotToReturn = second;

        RuntimeSnapshot refreshed = await provider.RefreshAsync(CancellationToken.None);

        Assert.Same(second, refreshed);
        Assert.Same(second, provider.GetSnapshot());
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
    public async Task RefreshAsync_DoesNotStartOrStopSupervisorLifecycle()
    {
        var supervisor = new StubAutomationHubSupervisor();
        using var provider = new SupervisorRuntimeSnapshotProvider(supervisor);
        supervisor.SnapshotToReturn = CreateSnapshot(CapturedAt, totalPlcs: 1);

        await provider.RefreshAsync(CancellationToken.None);

        Assert.Equal(0, supervisor.StartAsyncCallCount);
        Assert.Equal(0, supervisor.StopAsyncCallCount);
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
        public int StartAsyncCallCount { get; private set; }
        public int StopAsyncCallCount { get; private set; }
        public RuntimeSnapshot SnapshotToReturn { get; set; } = RuntimeSnapshot.Empty;
        public Exception? GetSnapshotAsyncException { get; set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartAsyncCallCount++;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopAsyncCallCount++;
            return Task.CompletedTask;
        }

        public Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            SnapshotChanged?.Invoke(
                this,
                new RuntimeSnapshotChangedEventArgs(snapshot, snapshot.CapturedAt));
        }

    }
}

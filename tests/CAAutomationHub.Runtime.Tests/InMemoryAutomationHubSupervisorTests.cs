using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime.Tests;

public sealed class InMemoryAutomationHubSupervisorTests
{
    [Fact]
    public async Task GetSnapshotAsync_ReturnsInitialEmptySnapshot()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();

        var snapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Same(RuntimeSnapshot.Empty, snapshot);
    }

    [Fact]
    public async Task GetSnapshotAsync_ThrowsWhenCancellationRequested()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => supervisor.GetSnapshotAsync(cts.Token));
    }

    [Fact]
    public async Task StartAsync_IsIdempotentAndPublishesOnlyWhenTransitioningToStarted()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        var changes = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => changes.Add(args);

        await supervisor.StartAsync(CancellationToken.None);
        await supervisor.StartAsync(CancellationToken.None);

        Assert.Single(changes);
        Assert.Equal(1, changes[0].Revision);
        Assert.NotSame(RuntimeSnapshot.Empty, changes[0].Snapshot);
    }

    [Fact]
    public async Task StartAsync_IncrementsRevisionForEachPublishedSnapshot()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        var changes = new List<RuntimeSnapshotChangedEventArgs>();
        supervisor.SnapshotChanged += (_, args) => changes.Add(args);

        await supervisor.StartAsync(CancellationToken.None);
        await supervisor.StopAsync(CancellationToken.None);
        await supervisor.StartAsync(CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Equal(1, changes[0].Revision);
        Assert.Equal(2, changes[1].Revision);
        Assert.NotSame(changes[0].Snapshot, changes[1].Snapshot);
    }

    [Fact]
    public async Task StartAsync_ThrowsWhenCancellationRequested()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => supervisor.StartAsync(cts.Token));
    }

    [Fact]
    public async Task StopAsync_IsIdempotentAndKeepsLastSnapshotWithoutPublishingRuntimeEvents()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        var snapshotChanges = 0;
        var runtimeEvents = 0;
        supervisor.SnapshotChanged += (_, _) => snapshotChanges++;
        supervisor.RuntimeEventRaised += (_, _) => runtimeEvents++;

        await supervisor.StartAsync(CancellationToken.None);
        var startedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);
        await supervisor.StopAsync(CancellationToken.None);
        await supervisor.StopAsync(CancellationToken.None);
        var stoppedSnapshot = await supervisor.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, snapshotChanges);
        Assert.Equal(0, runtimeEvents);
        Assert.Same(startedSnapshot, stoppedSnapshot);
    }

    [Fact]
    public async Task StopAsync_ThrowsWhenCancellationRequested()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => supervisor.StopAsync(cts.Token));
    }

    [Fact]
    public async Task SnapshotChangedEventArgs_UsesSingleCapturedAtForSnapshotHealthAndOccurredAt()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        RuntimeSnapshotChangedEventArgs? change = null;
        supervisor.SnapshotChanged += (_, args) => change = args;

        await supervisor.StartAsync(CancellationToken.None);

        Assert.NotNull(change);
        Assert.Equal(change.Snapshot.CapturedAt, change.Snapshot.Health.CapturedAt);
        Assert.Equal(change.Snapshot.CapturedAt, change.OccurredAt);
        Assert.Equal(1, change.Revision);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsUnsupportedFailureResult()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        var requestedAt = DateTimeOffset.UtcNow;
        var command = new RuntimeDashboardCommand(
            commandId: "cmd-1",
            kind: RuntimeDashboardCommandKind.TestConnection,
            plcId: "plc-1",
            requestedAt: requestedAt,
            parameters: null);

        var result = await supervisor.ExecuteAsync(command, CancellationToken.None);

        Assert.Equal(command.CommandId, result.CommandId);
        Assert.Equal(command.PlcId, result.PlcId);
        Assert.False(result.Success);
        Assert.Equal("Unsupported", result.Status);
        Assert.Equal("COMMAND_UNSUPPORTED", result.ErrorCode);
        Assert.True(result.CompletedAt >= requestedAt);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenCancellationRequested()
    {
        var supervisor = new InMemoryAutomationHubSupervisor();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var command = new RuntimeDashboardCommand(
            commandId: "cmd-1",
            kind: RuntimeDashboardCommandKind.TestConnection,
            plcId: null,
            requestedAt: DateTimeOffset.UtcNow,
            parameters: null);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => supervisor.ExecuteAsync(command, cts.Token));
    }
}

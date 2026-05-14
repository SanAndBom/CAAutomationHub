using CAAutomationHub.Contracts.Runtime;

namespace CAAutomationHub.Runtime;

/// <summary>
/// Provides a memory-backed <see cref="RuntimeSnapshot"/> source for the
/// production Runtime rail before PLC, driver, polling, and command dispatch
/// implementations exist.
/// </summary>
/// <remarks>
/// This supervisor is not a PLC fake and does not replace the WPF fake dashboard
/// adapter. It only publishes minimal Runtime snapshots from in-memory state.
/// </remarks>
public sealed class InMemoryAutomationHubSupervisor : IAutomationHubSupervisor
{
    private readonly object _gate = new();
    private RuntimeSnapshot _currentSnapshot = RuntimeSnapshot.Empty;
    private bool _started;
    private long _revision;
    private EventHandler<RuntimeEvent>? _runtimeEventRaised;

    /// <inheritdoc />
    public event EventHandler<RuntimeSnapshotChangedEventArgs>? SnapshotChanged;

    /// <inheritdoc />
    public event EventHandler<RuntimeEvent>? RuntimeEventRaised
    {
        add
        {
            lock (_gate)
            {
                _runtimeEventRaised += value;
            }
        }

        remove
        {
            lock (_gate)
            {
                _runtimeEventRaised -= value;
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RuntimeSnapshot snapshot;
        long revision;
        lock (_gate)
        {
            if (_started)
            {
                return Task.CompletedTask;
            }

            _started = true;
            snapshot = CreateEmptyRuntimeSnapshot(DateTimeOffset.UtcNow);
            _currentSnapshot = snapshot;
            revision = ++_revision;
        }

        SnapshotChanged?.Invoke(
            this,
            new RuntimeSnapshotChangedEventArgs(
                snapshot,
                occurredAt: snapshot.CapturedAt,
                revision: revision));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _started = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<RuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RuntimeSnapshot snapshot;
        lock (_gate)
        {
            snapshot = _currentSnapshot;
        }

        return Task.FromResult(snapshot);
    }

    /// <inheritdoc />
    public Task<RuntimeDashboardCommandResult> ExecuteAsync(
        RuntimeDashboardCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(command);

        var completedAt = DateTimeOffset.UtcNow;
        var result = new RuntimeDashboardCommandResult(
            CommandId: command.CommandId,
            Success: false,
            Status: "Unsupported",
            Message: "InMemoryAutomationHubSupervisor does not execute dashboard commands.",
            PlcId: command.PlcId,
            ErrorCode: "COMMAND_UNSUPPORTED",
            CompletedAt: completedAt);

        return Task.FromResult(result);
    }

    private static RuntimeSnapshot CreateEmptyRuntimeSnapshot(DateTimeOffset capturedAt)
    {
        return new RuntimeSnapshot(
            capturedAt: capturedAt,
            health: new RuntimeHealthState(
                TotalPlcs: 0,
                OnlineCount: 0,
                ReconnectingCount: 0,
                HealthyCount: 0,
                WarningCount: 0,
                CongestedCount: 0,
                ErrorCount: 0,
                InactiveCount: 0,
                CapturedAt: capturedAt),
            channels: Array.Empty<ChannelRuntimeState>(),
            recentEvents: Array.Empty<RuntimeEvent>());
    }

}

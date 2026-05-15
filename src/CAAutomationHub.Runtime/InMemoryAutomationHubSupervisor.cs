using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime.Channels;

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
    private readonly RuntimeChannelRegistry _channelRegistry;
    private RuntimeSnapshot _currentSnapshot;
    private bool _started;
    private long _revision;
    private EventHandler<RuntimeEvent>? _runtimeEventRaised;

    public InMemoryAutomationHubSupervisor()
        : this(new RuntimeChannelRegistry())
    {
    }

    public InMemoryAutomationHubSupervisor(RuntimeChannelRegistry channelRegistry)
    {
        _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
        _currentSnapshot = RuntimeSnapshot.Empty;
    }

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
            var capturedAt = DateTimeOffset.UtcNow;
            IReadOnlyList<ChannelRuntimeState> channels = _channelRegistry.GetStates(capturedAt);
            snapshot = CreateRuntimeSnapshot(capturedAt, channels);
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

    /// <summary>
    /// Refreshes and publishes the current Runtime snapshot from the in-memory
    /// channel registry.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the refresh operation.</param>
    /// <returns>The published Runtime snapshot.</returns>
    public Task<RuntimeSnapshot> RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var capturedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<ChannelRuntimeState> channels = _channelRegistry.GetStates(capturedAt);
        RuntimeSnapshot snapshot = CreateRuntimeSnapshot(capturedAt, channels);

        long revision;
        lock (_gate)
        {
            _currentSnapshot = snapshot;
            revision = ++_revision;
        }

        SnapshotChanged?.Invoke(
            this,
            new RuntimeSnapshotChangedEventArgs(
                snapshot,
                occurredAt: snapshot.CapturedAt,
                revision: revision));

        return Task.FromResult(snapshot);
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

    private static RuntimeSnapshot CreateRuntimeSnapshot(
        DateTimeOffset capturedAt,
        IReadOnlyList<ChannelRuntimeState> channels)
    {
        ChannelRuntimeState[] channelSnapshot = channels.ToArray();

        return new RuntimeSnapshot(
            capturedAt: capturedAt,
            health: CreateRuntimeHealthState(capturedAt, channelSnapshot),
            channels: channelSnapshot,
            recentEvents: Array.Empty<RuntimeEvent>());
    }

    private static RuntimeHealthState CreateRuntimeHealthState(
        DateTimeOffset capturedAt,
        IReadOnlyCollection<ChannelRuntimeState> channels)
    {
        return new RuntimeHealthState(
            TotalPlcs: channels.Count,
            OnlineCount: channels.Count(channel => channel.LinkState == PlcLinkState.Online),
            ReconnectingCount: channels.Count(channel => channel.LinkState == PlcLinkState.Reconnecting),
            HealthyCount: channels.Count(channel => channel.HealthSeverity == PlcHealthSeverity.Healthy),
            WarningCount: channels.Count(channel => channel.HealthSeverity == PlcHealthSeverity.Warning),
            CongestedCount: channels.Count(channel => channel.HealthSeverity == PlcHealthSeverity.Congested),
            ErrorCount: channels.Count(channel => channel.HealthSeverity == PlcHealthSeverity.Error),
            InactiveCount: channels.Count(channel => channel.HealthSeverity == PlcHealthSeverity.Inactive),
            CapturedAt: capturedAt);
    }
}

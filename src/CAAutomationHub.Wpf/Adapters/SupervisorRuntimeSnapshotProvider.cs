using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class SupervisorRuntimeSnapshotProvider : IRuntimeSnapshotProvider, IDisposable
{
    private readonly IAutomationHubSupervisor _supervisor;
    private readonly object _gate = new();
    private RuntimeSnapshot _currentSnapshot;

    public SupervisorRuntimeSnapshotProvider(IAutomationHubSupervisor supervisor)
    {
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _currentSnapshot = RuntimeSnapshot.Empty;
        _supervisor.SnapshotChanged += OnSnapshotChanged;
    }

    public RuntimeSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return _currentSnapshot;
        }
    }

    public async Task<RuntimeSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        RuntimeSnapshot snapshot = await _supervisor.GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        UpdateSnapshotIfLatest(snapshot);

        return GetSnapshot();
    }

    public void Dispose()
    {
        _supervisor.SnapshotChanged -= OnSnapshotChanged;
    }

    private void OnSnapshotChanged(object? sender, RuntimeSnapshotChangedEventArgs e)
    {
        UpdateSnapshotIfLatest(e.Snapshot);
    }

    private void UpdateSnapshotIfLatest(RuntimeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            if (snapshot.CapturedAt >= _currentSnapshot.CapturedAt)
            {
                _currentSnapshot = snapshot;
            }
        }
    }
}

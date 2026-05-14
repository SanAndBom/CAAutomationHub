using CAAutomationHub.Contracts.Runtime;
using CAAutomationHub.Runtime;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class SupervisorRuntimeSnapshotProvider : IRuntimeSnapshotProvider, IDisposable
{
    private readonly IAutomationHubSupervisor _supervisor;
    private RuntimeSnapshot _currentSnapshot;

    public SupervisorRuntimeSnapshotProvider(IAutomationHubSupervisor supervisor)
    {
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _currentSnapshot = RuntimeSnapshot.Empty;
        _supervisor.SnapshotChanged += OnSnapshotChanged;
    }

    public RuntimeSnapshot GetSnapshot()
    {
        return _currentSnapshot;
    }

    public void Dispose()
    {
        _supervisor.SnapshotChanged -= OnSnapshotChanged;
    }

    private void OnSnapshotChanged(object? sender, RuntimeSnapshotChangedEventArgs e)
    {
        _currentSnapshot = e.Snapshot;
    }
}

using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public interface IRuntimeDashboardEventSource
{
    /// <summary>
    /// Raised when a new dashboard snapshot is available; handlers must not assume UI thread affinity.
    /// </summary>
    event EventHandler<DashboardSnapshotChangedEventArgs>? SnapshotChanged;

    /// <summary>
    /// Raised when a runtime dashboard event is available; handlers must not assume UI thread affinity.
    /// </summary>
    event EventHandler<RuntimeDashboardEvent>? EventReceived;
}

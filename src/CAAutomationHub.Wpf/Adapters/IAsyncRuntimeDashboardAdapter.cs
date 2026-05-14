using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public interface IAsyncRuntimeDashboardAdapter
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

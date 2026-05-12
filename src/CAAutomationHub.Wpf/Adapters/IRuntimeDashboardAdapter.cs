using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public interface IRuntimeDashboardAdapter
{
    DashboardSnapshot GetSnapshot();
}

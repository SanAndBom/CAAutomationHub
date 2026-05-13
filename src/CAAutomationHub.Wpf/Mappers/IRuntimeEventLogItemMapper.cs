using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Mappers;

public interface IRuntimeEventLogItemMapper
{
    RuntimeEventLogItem Map(RuntimeDashboardEvent dashboardEvent);
}

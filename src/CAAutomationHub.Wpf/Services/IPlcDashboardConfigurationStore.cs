using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public interface IPlcDashboardConfigurationStore
{
    IReadOnlyList<PlcDashboardConfiguration> Load();
    Task<IReadOnlyList<PlcDashboardConfiguration>> LoadAsync(CancellationToken cancellationToken = default);
    void Save(IReadOnlyList<PlcDashboardConfiguration> configurations);
    Task SaveAsync(IReadOnlyList<PlcDashboardConfiguration> configurations, CancellationToken cancellationToken = default);
}

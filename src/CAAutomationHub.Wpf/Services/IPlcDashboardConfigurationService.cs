using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public interface IPlcDashboardConfigurationService
{
    IReadOnlyList<PlcDashboardConfiguration> GetPlcConfigurations();
    PlcDashboardConfiguration? GetPlcConfiguration(string plcId);
    void UpdatePlc(PlcDashboardConfiguration configuration);
    void DeletePlc(string plcId);
}

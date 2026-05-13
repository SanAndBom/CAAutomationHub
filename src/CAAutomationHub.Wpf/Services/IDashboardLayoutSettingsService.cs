using CAAutomationHub.Wpf.Models.Settings;

namespace CAAutomationHub.Wpf.Services;

public interface IDashboardLayoutSettingsService
{
    DashboardLayoutSettings Load();
    void Save(DashboardLayoutSettings settings);
}

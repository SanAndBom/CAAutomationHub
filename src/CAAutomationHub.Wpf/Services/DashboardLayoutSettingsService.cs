using System.IO;
using System.Text.Json;
using CAAutomationHub.Wpf.Models.Settings;

namespace CAAutomationHub.Wpf.Services;

public sealed class DashboardLayoutSettingsService : IDashboardLayoutSettingsService
{
    public const double MinCommunicationTrendHeight = 220;
    public const double MaxCommunicationTrendHeight = 420;
    public const double DefaultCommunicationTrendHeight = 300;
    private const string AppDirectoryName = "CAAutomationHub";
    private const string FileName = "dashboard-layout.json";
    private readonly string _settingsPath;

    public DashboardLayoutSettingsService()
        : this(CreateDefaultSettingsPath())
    {
    }

    public DashboardLayoutSettingsService(string settingsPath)
        => _settingsPath = settingsPath;

    public DashboardLayoutSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return CreateDefault();

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<DashboardLayoutSettings>(json);

            return Normalize(settings);
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(DashboardLayoutSettings settings)
    {
        try
        {
            var normalized = Normalize(settings);
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Layout persistence is non-critical; never block dashboard startup or interaction.
        }
    }

    public static double ClampCommunicationTrendHeight(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height)) return DefaultCommunicationTrendHeight;
        return Math.Clamp(height, MinCommunicationTrendHeight, MaxCommunicationTrendHeight);
    }

    private static DashboardLayoutSettings Normalize(DashboardLayoutSettings? settings)
        => new(settings is null
            ? DefaultCommunicationTrendHeight
            : ClampCommunicationTrendHeight(settings.CommunicationTrendHeight));

    private static DashboardLayoutSettings CreateDefault()
        => new(DefaultCommunicationTrendHeight);

    private static string CreateDefaultSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppDirectoryName, FileName);
    }
}

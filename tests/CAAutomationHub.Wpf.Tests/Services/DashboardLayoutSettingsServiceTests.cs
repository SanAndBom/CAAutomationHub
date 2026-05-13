using CAAutomationHub.Wpf.Services;
using CAAutomationHub.Wpf.Models.Settings;

namespace CAAutomationHub.Wpf.Tests.Services;

public sealed class DashboardLayoutSettingsServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _settingsPath;

    public DashboardLayoutSettingsServiceTests()
    {
        _settingsPath = Path.Combine(_tempRoot, "dashboard-layout.json");
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultTrendHeight()
    {
        var service = new DashboardLayoutSettingsService(_settingsPath);

        var settings = service.Load();

        Assert.Equal(DashboardLayoutSettingsService.DefaultCommunicationTrendHeight, settings.CommunicationTrendHeight);
    }

    [Fact]
    public void Save_ThenLoad_RestoresTrendHeight()
    {
        var service = new DashboardLayoutSettingsService(_settingsPath);

        service.Save(new DashboardLayoutSettings(360));

        var settings = service.Load();
        Assert.Equal(360, settings.CommunicationTrendHeight);
    }

    [Fact]
    public void Load_ClampsHeightBelowMinimum()
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(_settingsPath, """{"CommunicationTrendHeight":120}""");
        var service = new DashboardLayoutSettingsService(_settingsPath);

        var settings = service.Load();

        Assert.Equal(DashboardLayoutSettingsService.MinCommunicationTrendHeight, settings.CommunicationTrendHeight);
    }

    [Fact]
    public void Load_ClampsHeightAboveMaximum()
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(_settingsPath, """{"CommunicationTrendHeight":999}""");
        var service = new DashboardLayoutSettingsService(_settingsPath);

        var settings = service.Load();

        Assert.Equal(DashboardLayoutSettingsService.MaxCommunicationTrendHeight, settings.CommunicationTrendHeight);
    }

    [Fact]
    public void Load_WhenJsonIsInvalid_ReturnsDefaultTrendHeight()
    {
        Directory.CreateDirectory(_tempRoot);
        File.WriteAllText(_settingsPath, "{ invalid json");
        var service = new DashboardLayoutSettingsService(_settingsPath);

        var settings = service.Load();

        Assert.Equal(DashboardLayoutSettingsService.DefaultCommunicationTrendHeight, settings.CommunicationTrendHeight);
    }

    [Fact]
    public void Save_WhenDirectoryCannotBeCreated_DoesNotThrow()
    {
        Directory.CreateDirectory(_tempRoot);
        var fileAsDirectoryParent = Path.Combine(_tempRoot, "not-a-directory");
        File.WriteAllText(fileAsDirectoryParent, "occupied");
        var invalidPath = Path.Combine(fileAsDirectoryParent, "dashboard-layout.json");
        var service = new DashboardLayoutSettingsService(invalidPath);

        var exception = Record.Exception(() => service.Save(new DashboardLayoutSettings(340)));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true);
    }
}

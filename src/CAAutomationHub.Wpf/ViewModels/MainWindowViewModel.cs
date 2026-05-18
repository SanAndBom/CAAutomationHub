using System.IO;
using CAAutomationHub.PilotComposition.Configuration;
using CAAutomationHub.PilotComposition.Polling;
using CAAutomationHub.Wpf.Adapters;
using CAAutomationHub.Wpf.Services;
using CAAutomationHub.Wpf.ViewModels.Pilot;

namespace CAAutomationHub.Wpf.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private const string ConfigPathEnvironmentVariable = "CAAH_PILOT_CONFIG";

    public MainWindowViewModel()
        : this(CreateDashboardViewModel(), CreateFallbackPilotPolling(), "Fake pilot polling profile loaded.")
    {
    }

    private MainWindowViewModel(
        DashboardViewModel dashboard,
        PilotPollingViewModel pilotPolling,
        string pilotStatusMessage)
    {
        Dashboard = dashboard;
        PilotPolling = pilotPolling;
        PilotStatusMessage = pilotStatusMessage;
    }

    public DashboardViewModel Dashboard { get; }

    public PilotPollingViewModel PilotPolling { get; }

    public string PilotStatusMessage { get; }

    public static MainWindowViewModel CreateDefaultPilotLocal(
        string? explicitConfigurationPath = null,
        string? plcCardsConfigurationPath = null)
    {
        try
        {
            var dashboard = CreateDashboardViewModel(plcCardsConfigurationPath);
            var configurationPath = explicitConfigurationPath ?? ResolveLocalConfigurationPath();
            if (configurationPath is null)
            {
                return new MainWindowViewModel(
                    dashboard,
                    CreateFallbackPilotPolling(),
                    "Pilot local config not found; fake profile loaded.");
            }

            var configuration = PilotLocalConfigurationLoader.Load(configurationPath);
            var composition = PilotLocalComposition.Create(configuration);

            return new MainWindowViewModel(
                dashboard,
                new PilotPollingViewModel(composition.PollingService),
                composition.StatusMessage);
        }
        catch (Exception ex)
        {
            return new MainWindowViewModel(
                CreateDashboardViewModel(plcCardsConfigurationPath),
                CreateFallbackPilotPolling(),
                $"Pilot local config failed safely; fake profile loaded. {ex.Message}");
        }
    }

    private static string? ResolveLocalConfigurationPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(ConfigPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var root = FindRepositoryRoot();
        if (root is null)
        {
            return null;
        }

        var candidates = new[]
        {
            Path.Combine(root.FullName, "config", "pilot.local.json"),
            Path.Combine(root.FullName, ".local", "pilot.local.json"),
            Path.Combine(root.FullName, "appsettings.local.json")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static DirectoryInfo? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CAAutomationHub.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static PilotPollingViewModel CreateFallbackPilotPolling()
    {
        var configuration = new PilotLocalConfiguration
        {
            Profile = PilotProfileKind.Fake,
            Plc = new PilotPlcTargetConfiguration
            {
                TargetId = "fake-profile",
                Host = "localhost",
                Port = 2004,
                ReadStartVariable = "%DB10000",
                ReadWordCount = 90,
                StartSignalWordIndex = 83,
                CompleteSignalWordIndex = 84,
                LotId1WordOffset = 0,
                LotId2WordOffset = 10,
                LotIdWordLength = 6
            },
            Db = new PilotDatabaseConfiguration
            {
                Mode = PilotDatabaseMode.Fake,
                ConnectionStringEnvironmentVariable = "CAAH_WORKSTART_DB_CONNECTION_STRING"
            }
        };

        return new PilotPollingViewModel(PilotLocalComposition.Create(configuration).PollingService);
    }

    private static DashboardViewModel CreateDashboardViewModel(string? plcCardsConfigurationPath = null)
    {
        var store = string.IsNullOrWhiteSpace(plcCardsConfigurationPath)
            ? JsonPlcDashboardConfigurationStore.CreateDefault()
            : new JsonPlcDashboardConfigurationStore(plcCardsConfigurationPath);
        var adapter = new FakeDashboardRuntimeAdapter(store);

        return new DashboardViewModel(adapter, adapter);
    }
}

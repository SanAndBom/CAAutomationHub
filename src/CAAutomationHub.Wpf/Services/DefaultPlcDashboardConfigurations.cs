using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Services;

public static class DefaultPlcDashboardConfigurations
{
    public static List<PlcDashboardConfiguration> Create()
        => Enumerable.Range(1, 5)
            .Select(index => CreateDefaultConfiguration(
                index,
                $"Press Line PLC {index:00}",
                $"Line-{((index - 1) % 4) + 1}",
                $"Press line PLC {index:00} fake configuration",
                500 + (index * 50),
                isEnabled: index != 5))
            .ToList();

    public static PlcDashboardConfiguration CreateDefaultConfiguration(
        int index,
        string? plcName = null,
        string lineName = "Line-1",
        string description = "신규 PLC",
        int pollingIntervalMs = 1000,
        bool isEnabled = true)
        => new(
            $"PLC-{index:00}",
            plcName ?? $"PLC {index:00}",
            lineName,
            description,
            $"192.168.0.{20 + index}",
            2004,
            pollingIntervalMs,
            800,
            5,
            5,
            AutoReconnect: true,
            ConnectOnStartup: true,
            IsEnabled: isEnabled);

    public static int GetNextConfigurationIndex(IEnumerable<PlcDashboardConfiguration> configurations)
    {
        var maxIndex = configurations
            .Select(configuration => GetStableIndex(configuration.PlcId, fallbackIndex: 0))
            .DefaultIfEmpty(0)
            .Max();

        return maxIndex + 1;
    }

    public static int GetStableIndex(string plcId, int fallbackIndex)
    {
        const string prefix = "PLC-";
        return plcId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(plcId[prefix.Length..], out var index)
            ? index
            : fallbackIndex;
    }
}

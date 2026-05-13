using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Mappers;

public sealed class RuntimeEventLogItemMapper : IRuntimeEventLogItemMapper
{
    private const string DefaultPlcName = "Runtime";
    private const string DefaultCategory = "Runtime";
    private const string DefaultMessage = "Runtime event received.";

    public RuntimeEventLogItem Map(RuntimeDashboardEvent dashboardEvent)
    {
        ArgumentNullException.ThrowIfNull(dashboardEvent);

        var severity = MapSeverity(dashboardEvent.Severity);

        return new RuntimeEventLogItem(
            dashboardEvent.OccurredAt,
            severity,
            FirstTextOrDefault(DefaultPlcName, dashboardEvent.PlcName, dashboardEvent.Source),
            TextOrDefault(dashboardEvent.Category, DefaultCategory),
            TextOrDefault(dashboardEvent.Message, DefaultMessage),
            TextOrDefault(dashboardEvent.Status, GetDefaultStatus(severity)));
    }

    private static EventSeverity MapSeverity(string? severity)
    {
        return severity?.Trim().ToUpperInvariant() switch
        {
            "CRITICAL" or "ERROR" or "FATAL" => EventSeverity.Critical,
            "WARNING" or "WARN" => EventSeverity.Warning,
            "INFO" or "INFORMATION" => EventSeverity.Info,
            _ => EventSeverity.Info
        };
    }

    private static string GetDefaultStatus(EventSeverity severity)
    {
        return severity switch
        {
            EventSeverity.Critical => "Open",
            EventSeverity.Warning => "Watch",
            _ => "Live"
        };
    }

    private static string TextOrDefault(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static string FirstTextOrDefault(string defaultValue, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return defaultValue;
    }
}

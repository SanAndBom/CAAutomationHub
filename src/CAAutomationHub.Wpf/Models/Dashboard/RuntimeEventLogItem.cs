namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record RuntimeEventLogItem(
    DateTimeOffset OccurredAt,
    EventSeverity Severity,
    string PlcName,
    string Category,
    string Message,
    string Status)
{
    public string TimeText => OccurredAt.ToLocalTime().ToString("HH:mm:ss");
    public string SeverityText => Severity.ToString();
}

namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record RuntimeDashboardEvent(
    DateTimeOffset OccurredAt,
    string Severity,
    string Message);

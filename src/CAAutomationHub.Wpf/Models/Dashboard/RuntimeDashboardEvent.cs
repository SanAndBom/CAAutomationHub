namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record RuntimeDashboardEvent(
    DateTimeOffset OccurredAt,
    string Severity,
    string Message,
    string? Source = null,
    string? Category = null,
    string? PlcId = null,
    string? PlcName = null,
    string? Status = null);

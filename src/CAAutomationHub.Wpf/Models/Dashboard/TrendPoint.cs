namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record TrendPoint(
    DateTimeOffset OccurredAt,
    double ResponseMs,
    bool HasError,
    TrendMarkerKind MarkerKind = TrendMarkerKind.None,
    string? MarkerText = null);

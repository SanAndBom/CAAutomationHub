namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record CommunicationTrendSeries(
    string TargetId,
    string TargetName,
    PlcConnectionState State,
    bool IsWorst,
    IReadOnlyList<TrendPoint> Points);

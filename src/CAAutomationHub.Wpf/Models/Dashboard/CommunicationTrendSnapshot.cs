namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record CommunicationTrendSnapshot
{
    public CommunicationTrendSnapshot(
        string targetId,
        string targetName,
        bool isOverview,
        double WarningThresholdMs,
        double ErrorThresholdMs,
        IReadOnlyList<TrendPoint>? points,
        string? worstPlcId = null,
        string? worstPlcName = null,
        double? worstResponseMs = null,
        IReadOnlyList<CommunicationTrendSeries>? series = null,
        double? congestedThresholdMs = null)
    {
        TargetId = targetId;
        TargetName = targetName;
        IsOverview = isOverview;
        this.WarningThresholdMs = WarningThresholdMs;
        CongestedThresholdMs = congestedThresholdMs ?? ((WarningThresholdMs + ErrorThresholdMs) / 2);
        this.ErrorThresholdMs = ErrorThresholdMs;
        Points = points ?? Array.Empty<TrendPoint>();
        WorstPlcId = worstPlcId;
        WorstPlcName = worstPlcName;
        WorstResponseMs = worstResponseMs;
        Series = series ?? Array.Empty<CommunicationTrendSeries>();
    }

    public string TargetId { get; init; }
    public string TargetName { get; init; }
    public bool IsOverview { get; init; }
    public double WarningThresholdMs { get; init; }
    public double CongestedThresholdMs { get; init; }
    public double ErrorThresholdMs { get; init; }
    public IReadOnlyList<TrendPoint> Points { get; init; }
    public string? WorstPlcId { get; init; }
    public string? WorstPlcName { get; init; }
    public double? WorstResponseMs { get; init; }
    public IReadOnlyList<CommunicationTrendSeries> Series { get; init; }

    public static CommunicationTrendSnapshot Empty { get; } = CreateEmpty("overview", "전체 Overview", isOverview: true);

    public static CommunicationTrendSnapshot CreateEmpty(string targetId, string targetName, bool isOverview)
        => new(targetId, targetName, isOverview, WarningThresholdMs: 0, ErrorThresholdMs: 0, Array.Empty<TrendPoint>());
}

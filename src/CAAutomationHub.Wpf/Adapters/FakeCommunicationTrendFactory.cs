using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

internal static class FakeCommunicationTrendFactory
{
    private const int TrendPointCount = 360;
    private const double WarningThresholdMs = 250;
    private const double CongestedThresholdMs = 500;
    private const double ErrorThresholdMs = 750;

    public static CommunicationTrendSetSnapshot Create(IReadOnlyList<PlcCardSnapshot> cards, int tick)
    {
        var plcTrends = cards
            .Select((card, index) => CreatePlcTrend(card, index, tick))
            .ToArray();
        var worst = plcTrends
            .SelectMany(trend => trend.Points.Select(point => new { trend, point }))
            .MaxBy(item => item.point.ResponseMs);
        var series = plcTrends
            .Select(trend =>
            {
                var card = cards.First(item => item.PlcId == trend.TargetId);
                return new CommunicationTrendSeries(
                    trend.TargetId,
                    trend.TargetName,
                    card.ConnectionState,
                    trend.TargetId == worst?.trend.TargetId,
                    trend.Points);
            })
            .ToArray();

        var overviewPoints = Enumerable.Range(0, TrendPointCount)
            .Select(sampleIndex =>
            {
                var pointsAtSample = plcTrends
                    .Select(trend => trend.Points[sampleIndex])
                    .ToArray();
                var responseMs = pointsAtSample.Max(point => point.ResponseMs);
                var hasError = pointsAtSample.Any(point => point.HasError);
                var markerKind = hasError ? TrendMarkerKind.Error : TrendMarkerKind.None;

                return new TrendPoint(
                    pointsAtSample[0].OccurredAt,
                    responseMs,
                    hasError,
                    markerKind,
                    CreateMarkerText(markerKind, responseMs));
            })
            .ToArray();

        var overview = new CommunicationTrendSnapshot(
            "overview",
            "전체 PLC 통신 품질",
            isOverview: true,
            WarningThresholdMs,
            ErrorThresholdMs,
            overviewPoints,
            worst?.trend.TargetId,
            worst?.trend.TargetName,
            worst?.point.ResponseMs,
            series,
            CongestedThresholdMs);

        return new CommunicationTrendSetSnapshot(overview, plcTrends);
    }

    private static CommunicationTrendSnapshot CreatePlcTrend(PlcCardSnapshot card, int cardIndex, int tick)
    {
        var now = DateTimeOffset.UtcNow;
        var points = Enumerable.Range(0, TrendPointCount)
            .Select(sampleIndex =>
            {
                var ageFromNow = TrendPointCount - 1 - sampleIndex;
                var occurredAt = now.AddSeconds(-ageFromNow * 5);
                var wave = Math.Sin((sampleIndex + tick + (cardIndex * 11)) / 14.0) * 22;
                var pulse = ((sampleIndex + tick + cardIndex) % 47 == 0) ? 95 : 0;
                var baseResponse = GetBaseResponse(card);
                var responseMs = Math.Max(0, baseResponse + wave + pulse);
                var hasError = card.ConnectionState == PlcConnectionState.Error
                    ? (sampleIndex + tick + cardIndex) % 19 == 0
                    : card.ErrorCount > 0 && (sampleIndex + tick + cardIndex) % 71 == 0;
                var markerKind = GetMarkerKind(responseMs, hasError);

                return new TrendPoint(
                    occurredAt,
                    responseMs,
                    hasError,
                    markerKind,
                    CreateMarkerText(markerKind, responseMs));
            })
            .ToArray();

        return new CommunicationTrendSnapshot(
            card.PlcId,
            card.PlcName,
            isOverview: false,
            WarningThresholdMs,
            ErrorThresholdMs,
            points,
            congestedThresholdMs: CongestedThresholdMs);
    }

    private static double GetBaseResponse(PlcCardSnapshot card)
        => card.ConnectionState switch
        {
            PlcConnectionState.Inactive => 0,
            PlcConnectionState.Healthy => Math.Max(24, card.LastResponseMs),
            PlcConnectionState.Warning => Math.Max(260, card.LastResponseMs),
            PlcConnectionState.Congested => Math.Max(420, card.LastResponseMs),
            PlcConnectionState.Error => Math.Max(820, card.LastResponseMs),
            _ => Math.Max(0, card.LastResponseMs)
        };

    private static TrendMarkerKind GetMarkerKind(double responseMs, bool hasError)
    {
        if (hasError || responseMs > ErrorThresholdMs) return TrendMarkerKind.Error;
        if (responseMs > WarningThresholdMs) return TrendMarkerKind.Warning;
        return TrendMarkerKind.None;
    }

    private static string? CreateMarkerText(TrendMarkerKind markerKind, double responseMs)
        => markerKind switch
        {
            TrendMarkerKind.Error => $"Error {responseMs:0}ms",
            TrendMarkerKind.Warning => $"Warning {responseMs:0}ms",
            _ => null
        };
}

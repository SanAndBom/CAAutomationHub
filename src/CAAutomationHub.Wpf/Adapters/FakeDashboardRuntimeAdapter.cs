using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class FakeDashboardRuntimeAdapter : IRuntimeDashboardAdapter
{
    private const int TrendPointCount = 360;
    private const double WarningThresholdMs = 250;
    private const double CongestedThresholdMs = 500;
    private const double ErrorThresholdMs = 750;
    private int _snapshotIndex;

    public DashboardSnapshot GetSnapshot()
    {
        var tick = _snapshotIndex++;
        var jitter = (tick % 7) - 3;
        var congestionPhase = tick % 12;

        var cards = new List<PlcCardSnapshot>
        {
            CreateCard(1, PlcConnectionState.Healthy, 38 + jitter, 134 + tick, 132 + tick, 0),
            CreateCard(2, PlcConnectionState.Warning, 512 + (jitter * 9), 182 + tick, 176 + tick, tick % 10 == 0 ? 2 : 1),
            CreateCard(3, GetCyclingState(congestionPhase), 94 + (jitter * 6), 148 + tick, 140 + tick, congestionPhase >= 8 ? 1 : 0),
            CreateCard(4, PlcConnectionState.Error, 880 + (jitter * 12), 46 + tick, 39 + tick, 5 + (tick / 9)),
            CreateCard(5, PlcConnectionState.Inactive, 0, 0, 0, 0),
            CreateCard(6, PlcConnectionState.Healthy, 62 + (jitter * 4), 156 + tick, 154 + tick, 0),
            CreateCard(7, PlcConnectionState.Healthy, 71 + (jitter * 5), 168 + tick, 165 + tick, 0),
            CreateCard(8, PlcConnectionState.Warning, 248 + (jitter * 8), 116 + tick, 112 + tick, 2),
            CreateCard(9, PlcConnectionState.Congested, 430 + (jitter * 10), 88 + tick, 81 + tick, 3),
            CreateCard(10, PlcConnectionState.Healthy, 55 + (jitter * 4), 172 + tick, 170 + tick, 0)
        };

        var health = new RuntimeHealthSnapshot(
            cards.Count,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Healthy),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Warning),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Congested),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Error),
            DateTimeOffset.UtcNow);

        var trend = CreateTrendSet(cards, tick);

        return new DashboardSnapshot(health, cards, trend);
    }

    private static PlcConnectionState GetCyclingState(int phase)
    {
        if (phase < 4) return PlcConnectionState.Healthy;
        if (phase < 8) return PlcConnectionState.Warning;
        return PlcConnectionState.Congested;
    }

    private static PlcCardSnapshot CreateCard(
        int index,
        PlcConnectionState state,
        int lastResponseMs,
        int txPerMinute,
        int rxPerMinute,
        int errorCount)
        => new(
            $"PLC-{index:00}",
            $"Press Line PLC {index:00}",
            $"Line-{((index - 1) % 4) + 1}",
            state,
            $"192.168.0.{20 + index}",
            2004,
            500 + (index * 50),
            Math.Max(0, lastResponseMs),
            txPerMinute,
            rxPerMinute,
            errorCount);

    private static CommunicationTrendSetSnapshot CreateTrendSet(IReadOnlyList<PlcCardSnapshot> cards, int tick)
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

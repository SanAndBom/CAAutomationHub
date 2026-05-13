using CAAutomationHub.Wpf.Models.Dashboard;
using CAAutomationHub.Wpf.Services;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class FakeDashboardRuntimeAdapter : IRuntimeDashboardAdapter, IPlcDashboardConfigurationService
{
    private const int TrendPointCount = 360;
    private const double WarningThresholdMs = 250;
    private const double CongestedThresholdMs = 500;
    private const double ErrorThresholdMs = 750;
    private readonly object _syncRoot = new();
    private readonly List<PlcDashboardConfiguration> _configurations;
    private int _snapshotIndex;

    public FakeDashboardRuntimeAdapter()
        => _configurations = CreateDefaultConfigurations();

    public DashboardSnapshot GetSnapshot()
    {
        var tick = _snapshotIndex++;
        var jitter = (tick % 7) - 3;
        var congestionPhase = tick % 12;
        List<PlcDashboardConfiguration> configurations;

        lock (_syncRoot)
        {
            configurations = _configurations.ToList();
        }

        var cards = configurations
            .Select((configuration, index) => CreateCard(configuration, index + 1, congestionPhase, jitter, tick))
            .ToArray();

        var health = new RuntimeHealthSnapshot(
            cards.Length,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Healthy),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Warning),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Congested),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Error),
            DateTimeOffset.UtcNow,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Inactive));

        var trend = CreateTrendSet(cards, tick);

        return new DashboardSnapshot(health, cards, trend);
    }

    public IReadOnlyList<PlcDashboardConfiguration> GetPlcConfigurations()
    {
        lock (_syncRoot)
        {
            return _configurations.ToArray();
        }
    }

    public PlcDashboardConfiguration? GetPlcConfiguration(string plcId)
    {
        lock (_syncRoot)
        {
            return _configurations.FirstOrDefault(configuration => configuration.PlcId == plcId);
        }
    }

    public PlcDashboardConfiguration CreateDefaultPlcConfiguration()
    {
        lock (_syncRoot)
        {
            var nextIndex = GetNextConfigurationIndex(_configurations);
            return CreateDefaultConfiguration(nextIndex);
        }
    }

    public PlcDashboardConfiguration AddPlc(PlcDashboardConfiguration configuration)
    {
        lock (_syncRoot)
        {
            var nextIndex = GetNextConfigurationIndex(_configurations);
            var normalized = configuration with
            {
                PlcId = $"PLC-{nextIndex:00}",
                IpAddress = string.IsNullOrWhiteSpace(configuration.IpAddress)
                    ? $"192.168.0.{20 + nextIndex}"
                    : configuration.IpAddress
            };

            _configurations.Add(normalized);
            return normalized;
        }
    }

    public void UpdatePlc(PlcDashboardConfiguration configuration)
    {
        lock (_syncRoot)
        {
            var index = _configurations.FindIndex(item => item.PlcId == configuration.PlcId);
            if (index < 0) return;

            _configurations[index] = configuration;
        }
    }

    public void DeletePlc(string plcId)
    {
        lock (_syncRoot)
        {
            _configurations.RemoveAll(configuration => configuration.PlcId == plcId);
        }
    }

    private static List<PlcDashboardConfiguration> CreateDefaultConfigurations()
        => Enumerable.Range(1, 5)
            .Select(index => CreateDefaultConfiguration(
                index,
                $"Press Line PLC {index:00}",
                $"Line-{((index - 1) % 4) + 1}",
                $"Press line PLC {index:00} fake configuration",
                500 + (index * 50),
                isEnabled: index != 5))
            .ToList();

    private static PlcDashboardConfiguration CreateDefaultConfiguration(
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

    private static int GetNextConfigurationIndex(IEnumerable<PlcDashboardConfiguration> configurations)
    {
        var maxIndex = configurations
            .Select(configuration => GetStableIndex(configuration.PlcId, fallbackIndex: 0))
            .DefaultIfEmpty(0)
            .Max();

        return maxIndex + 1;
    }

    private static PlcConnectionState GetCyclingState(int phase)
    {
        if (phase < 4) return PlcConnectionState.Healthy;
        if (phase < 8) return PlcConnectionState.Warning;
        return PlcConnectionState.Congested;
    }

    private static PlcCardSnapshot CreateCard(
        PlcDashboardConfiguration configuration,
        int fallbackIndex,
        int congestionPhase,
        int jitter,
        int tick)
    {
        var index = GetStableIndex(configuration.PlcId, fallbackIndex);
        var state = configuration.IsEnabled ? GetState(index, congestionPhase) : PlcConnectionState.Inactive;
        var lastResponseMs = state switch
        {
            PlcConnectionState.Healthy => index switch
            {
                1 => 38 + jitter,
                6 => 62 + (jitter * 4),
                7 => 71 + (jitter * 5),
                10 => 55 + (jitter * 4),
                _ => 48 + (index * 4) + jitter
            },
            PlcConnectionState.Warning => index == 2 ? 512 + (jitter * 9) : 248 + (jitter * 8),
            PlcConnectionState.Congested => index == 9 ? 430 + (jitter * 10) : 94 + (jitter * 6),
            PlcConnectionState.Error => 880 + (jitter * 12),
            _ => 0
        };
        var txPerMinute = state switch
        {
            PlcConnectionState.Inactive => 0,
            PlcConnectionState.Error => 46 + tick,
            PlcConnectionState.Congested => index == 9 ? 88 + tick : 148 + tick,
            PlcConnectionState.Warning => index == 2 ? 182 + tick : 116 + tick,
            _ => 120 + (index * 6) + tick
        };
        var rxPerMinute = state == PlcConnectionState.Inactive ? 0 : Math.Max(0, txPerMinute - (index % 6));
        var errorCount = state switch
        {
            PlcConnectionState.Error => 5 + (tick / 9),
            PlcConnectionState.Warning => index == 2 && tick % 10 == 0 ? 2 : index == 2 ? 1 : 2,
            PlcConnectionState.Congested => index == 3 && congestionPhase >= 8 ? 1 : index == 9 ? 3 : 0,
            _ => 0
        };

        return new PlcCardSnapshot(
            configuration.PlcId,
            configuration.PlcName,
            configuration.LineName,
            state,
            configuration.IpAddress,
            configuration.Port,
            configuration.PollingIntervalMs,
            Math.Max(0, lastResponseMs),
            txPerMinute,
            rxPerMinute,
            errorCount);
    }

    private static PlcConnectionState GetState(int index, int congestionPhase)
        => index switch
        {
            2 or 8 => PlcConnectionState.Warning,
            3 => GetCyclingState(congestionPhase),
            4 => PlcConnectionState.Error,
            5 => PlcConnectionState.Inactive,
            9 => PlcConnectionState.Congested,
            _ => PlcConnectionState.Healthy
        };

    private static int GetStableIndex(string plcId, int fallbackIndex)
    {
        const string prefix = "PLC-";
        return plcId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(plcId[prefix.Length..], out var index)
            ? index
            : fallbackIndex;
    }

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

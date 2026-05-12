using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class FakeDashboardRuntimeAdapter : IRuntimeDashboardAdapter
{
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

        return new DashboardSnapshot(health, cards);
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
}

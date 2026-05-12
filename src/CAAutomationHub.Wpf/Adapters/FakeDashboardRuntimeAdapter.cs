using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class FakeDashboardRuntimeAdapter : IRuntimeDashboardAdapter
{
    public DashboardSnapshot GetSnapshot()
    {
        var cards = Enumerable.Range(1, 10)
            .Select(i => new PlcCardSnapshot(
                $"PLC-{i:00}",
                $"Press Line PLC {i:00}",
                $"Line-{(i % 4) + 1}",
                (PlcConnectionState)(i % 5),
                $"192.168.0.{20 + i}",
                2004,
                500 + (i * 50),
                25 + (i * 8),
                120 + (i * 5),
                118 + (i * 4),
                i % 3))
            .ToList();

        var health = new RuntimeHealthSnapshot(
            cards.Count,
            cards.Count(c => c.ConnectionState == PlcConnectionState.Healthy),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Warning),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Congested),
            cards.Count(c => c.ConnectionState == PlcConnectionState.Error),
            DateTimeOffset.UtcNow);

        return new DashboardSnapshot(health, cards);
    }
}

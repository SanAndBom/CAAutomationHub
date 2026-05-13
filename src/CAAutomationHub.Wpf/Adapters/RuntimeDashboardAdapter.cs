using CAAutomationHub.Wpf.Models.Dashboard;

namespace CAAutomationHub.Wpf.Adapters;

public sealed class RuntimeDashboardAdapter : IRuntimeDashboardAdapter
{
    public DashboardSnapshot GetSnapshot()
    {
        // AH-WPF-05 skeleton only: this is not a real Runtime connection.
        // Do not reference Runtime, XgtChannelRunner, XgtDriverCore, or FakePlc here yet.
        // This adapter is the future boundary for translating Runtime state into DashboardSnapshot.
        var health = new RuntimeHealthSnapshot(
            TotalPlcs: 0,
            HealthyCount: 0,
            WarningCount: 0,
            CongestedCount: 0,
            ErrorCount: 0,
            SnapshotTime: DateTimeOffset.UtcNow);

        return new DashboardSnapshot(health, Array.Empty<PlcCardSnapshot>());
    }
}

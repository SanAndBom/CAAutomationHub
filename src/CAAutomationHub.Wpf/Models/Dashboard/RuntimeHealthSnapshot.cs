namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record RuntimeHealthSnapshot(
    int TotalPlcs,
    int HealthyCount,
    int WarningCount,
    int CongestedCount,
    int ErrorCount,
    DateTimeOffset SnapshotTime,
    int InactiveCount = 0);

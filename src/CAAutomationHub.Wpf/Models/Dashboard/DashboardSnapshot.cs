namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record DashboardSnapshot(
    RuntimeHealthSnapshot Health,
    IReadOnlyList<PlcCardSnapshot> PlcCards);

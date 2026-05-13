namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record DashboardSnapshot(
    RuntimeHealthSnapshot Health,
    IReadOnlyList<PlcCardSnapshot> PlcCards,
    CommunicationTrendSetSnapshot CommunicationTrend)
{
    public DashboardSnapshot(RuntimeHealthSnapshot health, IReadOnlyList<PlcCardSnapshot> plcCards)
        : this(health, plcCards, CommunicationTrendSetSnapshot.Empty)
    {
    }
}

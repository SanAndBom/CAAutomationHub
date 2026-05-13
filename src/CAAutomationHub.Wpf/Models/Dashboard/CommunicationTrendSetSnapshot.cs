namespace CAAutomationHub.Wpf.Models.Dashboard;

public sealed record CommunicationTrendSetSnapshot
{
    public CommunicationTrendSetSnapshot(
        CommunicationTrendSnapshot? overview,
        IReadOnlyList<CommunicationTrendSnapshot>? plcTrends)
    {
        Overview = overview ?? CommunicationTrendSnapshot.Empty;
        PlcTrends = plcTrends ?? Array.Empty<CommunicationTrendSnapshot>();
    }

    public CommunicationTrendSnapshot Overview { get; init; }
    public IReadOnlyList<CommunicationTrendSnapshot> PlcTrends { get; init; }

    public static CommunicationTrendSetSnapshot Empty { get; } = new(CommunicationTrendSnapshot.Empty, Array.Empty<CommunicationTrendSnapshot>());
}

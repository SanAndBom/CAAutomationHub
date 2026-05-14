namespace CAAutomationHub.Contracts.Runtime;

public sealed record RuntimeHealthState(
    int TotalPlcs,
    int OnlineCount,
    int ReconnectingCount,
    int HealthyCount,
    int WarningCount,
    int CongestedCount,
    int ErrorCount,
    int InactiveCount,
    DateTimeOffset CapturedAt)
{
    public static RuntimeHealthState Empty { get; } = new(
        TotalPlcs: 0,
        OnlineCount: 0,
        ReconnectingCount: 0,
        HealthyCount: 0,
        WarningCount: 0,
        CongestedCount: 0,
        ErrorCount: 0,
        InactiveCount: 0,
        CapturedAt: DateTimeOffset.UnixEpoch);
}

namespace CAAutomationHub.PilotApp.Polling;

public sealed record PilotPollingOptions
{
    public required string TargetId { get; init; }

    public string? TargetLabel { get; init; }

    public string? DisplayName { get; init; }

    public string? LineName { get; init; }

    public string? HostPort { get; init; }

    public int MaxLogEntries { get; init; } = 100;

    public int MaxTrendPoints { get; init; } = 50;
}

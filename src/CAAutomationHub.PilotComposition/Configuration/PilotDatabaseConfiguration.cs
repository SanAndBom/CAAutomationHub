namespace CAAutomationHub.PilotComposition.Configuration;

public sealed record PilotDatabaseConfiguration
{
    public PilotDatabaseMode Mode { get; init; } = PilotDatabaseMode.Fake;

    public string? ConnectionEnvironmentVariable { get; init; }
}

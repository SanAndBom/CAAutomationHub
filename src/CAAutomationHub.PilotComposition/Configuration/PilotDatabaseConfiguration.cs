namespace CAAutomationHub.PilotComposition.Configuration;

public sealed record PilotDatabaseConfiguration
{
    public PilotDatabaseMode Mode { get; init; } = PilotDatabaseMode.Fake;

    public string? ConnectionStringEnvironmentVariable { get; init; }

    public string? ConnectionEnvironmentVariable { get; init; }

    public string? EffectiveConnectionStringEnvironmentVariable =>
        !string.IsNullOrWhiteSpace(ConnectionStringEnvironmentVariable)
            ? ConnectionStringEnvironmentVariable
            : ConnectionEnvironmentVariable;
}

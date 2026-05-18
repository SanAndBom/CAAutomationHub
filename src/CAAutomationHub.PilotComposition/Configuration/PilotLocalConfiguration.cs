namespace CAAutomationHub.PilotComposition.Configuration;

public sealed record PilotLocalConfiguration
{
    public PilotProfileKind Profile { get; init; } = PilotProfileKind.Fake;

    public required PilotPlcTargetConfiguration Plc { get; init; }

    public required PilotDatabaseConfiguration Db { get; init; }
}

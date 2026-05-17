namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartPayloadBuildResult
{
    public required byte[] PayloadBytes { get; init; }

    public required int WordCount { get; init; }

    public required IReadOnlyList<WorkStartPayloadField> Fields { get; init; }
}

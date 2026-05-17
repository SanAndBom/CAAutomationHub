namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartPayloadBuildOptions
{
    public const int DefaultWordCount = 70;

    public static WorkStartPayloadBuildOptions Default { get; } = new();

    public int WordCount { get; init; } = DefaultWordCount;
}

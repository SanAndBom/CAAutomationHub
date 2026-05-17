namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartPayloadField
{
    public required string Name { get; init; }

    public required int StartWordOffset { get; init; }

    public required int WordLength { get; init; }

    public required byte[] Bytes { get; init; }
}

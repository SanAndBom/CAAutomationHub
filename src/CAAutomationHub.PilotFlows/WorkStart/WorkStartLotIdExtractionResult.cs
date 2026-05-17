namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartLotIdExtractionResult
{
    public required string LotId { get; init; }

    public required int WordOffset { get; init; }

    public required int WordLength { get; init; }

    public required bool IsInRange { get; init; }
}

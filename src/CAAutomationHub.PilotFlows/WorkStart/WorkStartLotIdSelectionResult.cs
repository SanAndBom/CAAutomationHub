namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartLotIdSelectionResult
{
    public required string? SelectedLotId { get; init; }

    public required WorkStartLotIdSelectionSource Source { get; init; }

    public bool HasSelection => SelectedLotId is not null;
}

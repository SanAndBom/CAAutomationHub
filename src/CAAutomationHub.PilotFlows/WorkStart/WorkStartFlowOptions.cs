namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed record WorkStartFlowOptions
{
    public static WorkStartFlowOptions Default { get; } = new();

    public int StartSignalWordIndex { get; init; } = WorkStartReadBlockLayout.DefaultStartSignalWordIndex;

    public int LotId1WordOffset { get; init; } = WorkStartReadBlockLayout.DefaultLotId1WordOffset;

    public int LotId2WordOffset { get; init; } = WorkStartReadBlockLayout.DefaultLotId2WordOffset;

    public int LotIdWordLength { get; init; } = WorkStartReadBlockLayout.DefaultLotIdWordLength;

    public WorkStartPayloadBuildOptions PayloadBuildOptions { get; init; } = WorkStartPayloadBuildOptions.Default;
}

namespace CAAutomationHub.PilotFlows.WorkStart;

public sealed class WorkStartAckOffOptions
{
    public static WorkStartAckOffOptions Default { get; } = new();

    public int StartSignalWordIndex { get; init; } = WorkStartReadBlockLayout.DefaultStartSignalWordIndex;
}
